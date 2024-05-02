//
// NFS Server
//
// Copyright (c) 2004-2012 Pete Barber
//
// Licensed under the The Code Project Open License (CPOL.html)
// http://www.codeproject.com/info/cpol10.aspx 
//
using System;
using System.IO;

using RPCV2Lib;

namespace MountV1;
public enum Procs : uint
{
    Null = 0,
    Mount = 1,
    Dump = 2,
    UMount = 3,
    UMountAll = 4,
    Export = 5
}
/// <summary>
/// Summary description for server.
/// </summary>
public class MountD : RpcD
{
    public MountD() : base(Ports.mountd, Progs.mountd) { }

    protected override void Proc(uint proc, RpcCracker cracker, RpcPacker reply)
    {
        switch (proc)
        {
            case (uint)Procs.Null:
                throw new BadProc();
            case (uint)Procs.Mount:
                Mount(cracker, reply);
                break;
            case (uint)Procs.Dump:
                throw new BadProc();
            case (uint)Procs.UMount:
                UMount(cracker, reply);
                break;
            case (uint)Procs.UMountAll:
                throw new BadProc();
            case (uint)Procs.Export:
                throw new BadProc();
            default:
                throw new BadProc();
        }
    }

    private void Mount(RpcCracker cracker, RpcPacker reply)
    {
        uint length = cracker.get_uint32();

        var dirPath = "";

        for (uint i = 0; i < length; ++i)
            dirPath += cracker.get_char();

        Console.WriteLine("Mount {0}:{1}", length, dirPath);


        if (Directory.Exists(dirPath) == false)
        {
            reply.SetUint32(2); // Errno for no such file or directory
            reply.SetUint32(0); // Where fh would go
        }
        else
        {
            FHandle fh = FileTable.Add(new FileEntry(dirPath));

            reply.SetUint32(0);     // Success

            fh.Pack(reply);
        }
    }

    private void UMount(RpcCracker cracker, RpcPacker reply)
    {
        var length = cracker.get_uint32();

        var dirPath = "";

        for (uint i = 0; i < length; ++i)
            dirPath += cracker.get_char();

        Console.WriteLine("UMount {0}:{1}", length, dirPath);
#if FOO
		uint fh = fileHandles.Find(

		if (fileHandles.Remove(dirPath) == false)
			Console.WriteLine("{0} not mounted", dirPath);
#endif
    }
}
