//
// NFS Server
//
// Copyright (c) 2004-2012 Pete Barber
//
// Licensed under the The Code Project Open License (CPOL.html)
// http://www.codeproject.com/info/cpol10.aspx 
//
using RPCV2Lib;

namespace NFSV2;

/// <summary>
/// Summary description for sattr.
/// </summary>
public class Sattr
{
	uint	mode;
	uint	uid;
	uint	gid;
	uint	size;
	Timeval	atime;
	Timeval mtime;

	public Sattr(RpcCracker cracker)
	{
		mode	= cracker.get_uint32();
		uid		= cracker.get_uint32();
		gid		= cracker.get_uint32();
		size	= cracker.get_uint32();
		atime	= new Timeval(cracker);
		mtime	= new Timeval(cracker);
	}

    public uint Mode => mode;

    public uint Size => size;

    public Timeval AccessTime => atime;

    public Timeval ModTime => mtime;
}
