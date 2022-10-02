﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AuroraLip.Common.Extensions
{
    public static class PathEX
    {
        public const char ExtensionSeparatorChar = '.';

        public static ReadOnlySpan<char> GetRelativePath(in ReadOnlySpan<char> path, in ReadOnlySpan<char> MainPath)
        {
            if (path.StartsWith(MainPath))
                return path.Slice(MainPath.Length).TrimStart(Path.DirectorySeparatorChar);
            return path;
        }

        public static ReadOnlySpan<char> WithoutExtension(in ReadOnlySpan<char> path)
        {
            if (path.LastIndexOf(ExtensionSeparatorChar) > path.LastIndexOf(Path.DirectorySeparatorChar))
                return path.Slice(0, path.LastIndexOf(ExtensionSeparatorChar));
            return path;
        }

        public static bool CheckInvalidPathChars(in ReadOnlySpan<char> path)
            => path.IndexOfAny(Path.GetInvalidPathChars()) == 0;

    }
}
