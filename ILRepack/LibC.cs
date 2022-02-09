using System;
using System.Runtime.InteropServices;

namespace ILRepacking
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    internal static class LibC
    {
        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string path, uint mode);

        [DllImport("libc", SetLastError = true)]
        private static extern int uname(IntPtr buf);

        private class RuntimeInfo
        {
            public enum ProcessorArchitecture
            {
                Amd64,
                Arm64
            }

            public enum OsKind
            {
                Linux,
                MacOs
            }

            public readonly ProcessorArchitecture Architecture;
            public readonly OsKind Os;

            private RuntimeInfo(OsKind os, ProcessorArchitecture processorArchitecture)
            {
                Os = os;
                Architecture = processorArchitecture;
            }

            public static readonly Lazy<RuntimeInfo> Current = new Lazy<RuntimeInfo>(() =>
            {
                var buf = IntPtr.Zero;
                try
                {
                    buf = Marshal.AllocHGlobal(8192);
                    var rc = uname(buf);
                    if (rc != 0)
                    {
                        throw new Exception("uname() from libc returned " + rc);
                    }

                    OsKind platform;
                    switch (Marshal.PtrToStringAnsi(buf))
                    {
                        case "Darwin":
                            platform = OsKind.MacOs;
                            break;
                        case "Linux":
                            platform = OsKind.Linux;
                            break;
                        default:
                            throw new PlatformNotSupportedException();
                    }


                    var isMacOs = platform == OsKind.MacOs;
                    var nameLen = isMacOs ? 256 : 65;
                    const int machineIndex = 4;

                    var arch = Marshal.PtrToStringAnsi(buf + machineIndex * nameLen);
                    if (isMacOs)
                    {
                        switch (arch)
                        {
                            case "arm64":
                                return new RuntimeInfo(OsKind.MacOs, ProcessorArchitecture.Arm64);
                            case "x86_64":
                                return new RuntimeInfo(OsKind.MacOs, ProcessorArchitecture.Amd64);
                            default:
                                throw new PlatformNotSupportedException();
                        }
                    }
                    else
                    {
                        switch (arch)
                        {
                            case "aarch64":
                                return new RuntimeInfo(OsKind.Linux, ProcessorArchitecture.Arm64);
                            case "x86_64":
                                return new RuntimeInfo(OsKind.Linux, ProcessorArchitecture.Amd64);
                            default:
                                throw new PlatformNotSupportedException();
                        }
                    }
                }
                finally
                {
                    if (buf != IntPtr.Zero)
                        Marshal.FreeHGlobal(buf);
                }
            });
        }

        public static int ChMod(string path, uint mode)
        {
            AssertIsUnix();
            return chmod(path, mode);
        }

        public static int Stat(string path, out XPlatLayout.stat stat)
        {
            AssertIsUnix();
            if (RuntimeInfo.Current.Value.Os == RuntimeInfo.OsKind.MacOs)
            {
                switch (RuntimeInfo.Current.Value.Architecture)
                {
                    case RuntimeInfo.ProcessorArchitecture.Amd64:
                        return MacOs.stat_x64(path, out stat);
                    case RuntimeInfo.ProcessorArchitecture.Arm64:
                        return MacOs.stat_arm64(path, out stat);
                    default:
                        throw new PlatformNotSupportedException();
                }
            }

            switch (RuntimeInfo.Current.Value.Architecture)
            {
                case RuntimeInfo.ProcessorArchitecture.Amd64:
                    return Linux.stat_x64(path, out stat);
                case RuntimeInfo.ProcessorArchitecture.Arm64:
                    return Linux.stat_arm64(path, out stat);
                default:
                    throw new PlatformNotSupportedException();
            }
        }


        private static void AssertIsUnix()
        {
            if (Environment.OSVersion.Platform != PlatformID.MacOSX
                && Environment.OSVersion.Platform != PlatformID.Unix
                && Environment.Is64BitOperatingSystem)
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static class XPlatLayout
        {
            public struct TimeSpec
            {
                public long tv_sec;
                public ulong tv_nsec;
            }

            public struct stat
            {
                public ulong st_dev;
                public ulong st_ino;
                public uint st_mode;
                public ulong st_nlink;
                public uint st_uid;
                public uint st_gid;
                public ulong st_rdev;
                public long st_size;
                public TimeSpec st_atim;
                public TimeSpec st_mtim;
                public TimeSpec st_ctim;
                public long st_blksize;
                public long st_blocks;
            }
        }

        private static class Linux
        {
            private const string LibraryName = "libc";
            private const int Ver = 1;

            private static class Interop
            {
                [DllImport(LibraryName, SetLastError = true)]
                public static extern unsafe int __xstat(int ver, string path, void* stat);
            }

            private static class Layout
            {
                public static class X64
                {
                    [StructLayout(LayoutKind.Explicit, Size = 144)]
                    public struct stat
                    {
                        [FieldOffset(0)] public ulong st_dev;
                        [FieldOffset(8)] public ulong st_ino;
                        [FieldOffset(16)] public ulong st_nlink;
                        [FieldOffset(24)] public uint st_mode;
                        [FieldOffset(28)] public uint st_uid;
                        [FieldOffset(32)] public uint st_gid;
                        [FieldOffset(40)] public ulong st_rdev;
                        [FieldOffset(48)] public long st_size;
                        [FieldOffset(56)] public long st_blksize;
                        [FieldOffset(64)] public long st_blocks;
                        [FieldOffset(72)] public Bitness64.timespec st_atim;
                        [FieldOffset(88)] public Bitness64.timespec st_mtim;
                        [FieldOffset(104)] public Bitness64.timespec st_ctim;
                    }
                }

                public static class Arm64
                {
                    [StructLayout(LayoutKind.Explicit, Size = 128)]
                    public struct stat
                    {
                        [FieldOffset(0)] public ulong st_dev;
                        [FieldOffset(8)] public ulong st_ino;
                        [FieldOffset(16)] public uint st_mode;
                        [FieldOffset(20)] public uint st_nlink;
                        [FieldOffset(24)] public uint st_uid;
                        [FieldOffset(28)] public uint st_gid;
                        [FieldOffset(32)] public ulong st_rdev;
                        [FieldOffset(48)] public long st_size;
                        [FieldOffset(56)] public int st_blksize;
                        [FieldOffset(64)] public long st_blocks;
                        [FieldOffset(72)] public Bitness64.timespec st_atim;
                        [FieldOffset(88)] public Bitness64.timespec st_mtim;
                        [FieldOffset(104)] public Bitness64.timespec st_ctim;
                    }
                }

                public static class Bitness64
                {
                    [StructLayout(LayoutKind.Explicit, Size = 16)]
                    public struct timespec
                    {
                        [FieldOffset(0)] public long tv_sec;
                        [FieldOffset(8)] public ulong tv_nsec;
                    }
                }
            }

            public static unsafe int stat_x64(string path, out XPlatLayout.stat stat)
            {
                Layout.X64.stat tmp;
                var errno = Interop.__xstat(Ver, path, &tmp) != 0 ? Marshal.GetLastWin32Error() : 0;
                stat = new XPlatLayout.stat
                {
                    st_dev = tmp.st_dev,
                    st_ino = tmp.st_ino,
                    st_mode = tmp.st_mode,
                    st_nlink = tmp.st_nlink,
                    st_uid = tmp.st_uid,
                    st_gid = tmp.st_gid,
                    st_rdev = tmp.st_rdev,
                    st_size = tmp.st_size,
                    st_atim = new XPlatLayout.TimeSpec
                    {
                        tv_sec = tmp.st_atim.tv_sec,
                        tv_nsec = tmp.st_atim.tv_nsec,
                    },
                    st_mtim = new XPlatLayout.TimeSpec
                    {
                        tv_sec = tmp.st_mtim.tv_sec,
                        tv_nsec = tmp.st_mtim.tv_nsec,
                    },
                    st_ctim = new XPlatLayout.TimeSpec
                    {
                        tv_sec = tmp.st_ctim.tv_sec,
                        tv_nsec = tmp.st_ctim.tv_nsec,
                    },
                    st_blksize = tmp.st_blksize,
                    st_blocks = tmp.st_blocks
                };
                return errno;
            }

            public static unsafe int stat_arm64(string path, out XPlatLayout.stat stat)
            {
                Layout.Arm64.stat tmp;
                var errno = Interop.__xstat(0, path, &tmp) != 0 ? Marshal.GetLastWin32Error() : 0;
                stat = new XPlatLayout.stat
                {
                    st_dev = tmp.st_dev,
                    st_ino = tmp.st_ino,
                    st_mode = tmp.st_mode,
                    st_nlink = tmp.st_nlink,
                    st_uid = tmp.st_uid,
                    st_gid = tmp.st_gid,
                    st_rdev = tmp.st_rdev,
                    st_size = tmp.st_size,
                    st_atim = new XPlatLayout.TimeSpec
                    {
                        tv_sec = tmp.st_atim.tv_sec,
                        tv_nsec = tmp.st_atim.tv_nsec,
                    },
                    st_mtim = new XPlatLayout.TimeSpec
                    {
                        tv_sec = tmp.st_mtim.tv_sec,
                        tv_nsec = tmp.st_mtim.tv_nsec,
                    },
                    st_ctim = new XPlatLayout.TimeSpec
                    {
                        tv_sec = tmp.st_ctim.tv_sec,
                        tv_nsec = tmp.st_ctim.tv_nsec,
                    },
                    st_blksize = tmp.st_blksize,
                    st_blocks = tmp.st_blocks
                };
                return errno;
            }
        }

        private static class MacOs
        {
            private static class Interop
            {
                private const string LibraryName = "/usr/lib/system/libsystem_kernel.dylib";

                [DllImport(LibraryName, EntryPoint = "stat$INODE64", SetLastError = true)]
                public static extern unsafe int stat_INODE64(string path, void* stat);

                [DllImport(LibraryName, SetLastError = true)]
                public static extern unsafe int stat(string path, void* stat);
            }

            private static class Layout
            {
                [StructLayout(LayoutKind.Explicit, Size = 144)]
                internal struct stat
                {
                    [FieldOffset(0)] public int st_dev;
                    [FieldOffset(4)] public ushort st_mode;
                    [FieldOffset(6)] public ushort st_nlink;
                    [FieldOffset(8)] public ulong st_ino;
                    [FieldOffset(16)] public uint st_uid;
                    [FieldOffset(20)] public uint st_gid;
                    [FieldOffset(24)] public long st_rdev;
                    [FieldOffset(32)] public timespec st_atimespec;
                    [FieldOffset(48)] public timespec st_mtimespec;
                    [FieldOffset(64)] public timespec st_ctimespec;
                    [FieldOffset(80)] public timespec st_birthtimespec;
                    [FieldOffset(96)] public long st_size;
                    [FieldOffset(104)] public long st_blocks;
                    [FieldOffset(112)] public int st_blksize;
                    [FieldOffset(116)] public uint st_flags;
                    [FieldOffset(120)] public uint st_gen;
                    [FieldOffset(124)] private int st_lspare;
                }

                [StructLayout(LayoutKind.Explicit, Size = 16)]
                public struct timespec
                {
                    [FieldOffset(0)] public long tv_sec;
                    [FieldOffset(8)] public ulong tv_nsec;
                }
            }

            public static unsafe int stat_x64(string path, out XPlatLayout.stat stat)
            {
                Layout.stat tmp;
                var errno = Interop.stat_INODE64(path, &tmp) != 0 ? Marshal.GetLastWin32Error() : 0;
                stat = new XPlatLayout.stat
                {
                    st_dev = (ulong)tmp.st_dev,
                    st_ino = tmp.st_ino,
                    st_mode = tmp.st_mode,
                    st_nlink = tmp.st_nlink,
                    st_uid = tmp.st_uid,
                    st_gid = tmp.st_gid,
                    st_rdev = (ulong)tmp.st_rdev,
                    st_size = tmp.st_size,
                    st_atim = new XPlatLayout.TimeSpec
                    {
                        tv_sec = tmp.st_atimespec.tv_sec,
                        tv_nsec = tmp.st_atimespec.tv_nsec,
                    },
                    st_mtim = new XPlatLayout.TimeSpec
                    {
                        tv_sec = tmp.st_mtimespec.tv_sec,
                        tv_nsec = tmp.st_mtimespec.tv_nsec,
                    },
                    st_ctim = new XPlatLayout.TimeSpec
                    {
                        tv_sec = tmp.st_ctimespec.tv_sec,
                        tv_nsec = tmp.st_ctimespec.tv_nsec,
                    },
                    st_blksize = tmp.st_blksize,
                    st_blocks = tmp.st_blocks
                };
                return errno;
            }

            public static unsafe int stat_arm64(string path, out XPlatLayout.stat stat)
            {
                Layout.stat tmp;
                var errno = Interop.stat(path, &tmp) != 0 ? Marshal.GetLastWin32Error() : 0;
                stat = new XPlatLayout.stat
                {
                    st_dev = (ulong)tmp.st_dev,
                    st_ino = tmp.st_ino,
                    st_mode = tmp.st_mode,
                    st_nlink = tmp.st_nlink,
                    st_uid = tmp.st_uid,
                    st_gid = tmp.st_gid,
                    st_rdev = (ulong)tmp.st_rdev,
                    st_size = tmp.st_size,
                    st_atim = new XPlatLayout.TimeSpec
                    {
                        tv_sec = tmp.st_atimespec.tv_sec,
                        tv_nsec = tmp.st_atimespec.tv_nsec,
                    },
                    st_mtim = new XPlatLayout.TimeSpec
                    {
                        tv_sec = tmp.st_mtimespec.tv_sec,
                        tv_nsec = tmp.st_mtimespec.tv_nsec,
                    },
                    st_ctim = new XPlatLayout.TimeSpec
                    {
                        tv_sec = tmp.st_ctimespec.tv_sec,
                        tv_nsec = tmp.st_ctimespec.tv_nsec,
                    },
                    st_blksize = tmp.st_blksize,
                    st_blocks = tmp.st_blocks
                };
                return errno;
            }
        }
    }
}