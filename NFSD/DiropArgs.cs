//
// NFS Server
//
// Copyright (c) 2004-2012 Pete Barber
//
// Licensed under the The Code Project Open License (CPOL.html)
// http://www.codeproject.com/info/cpol10.aspx 
//
using System;
using RPCV2Lib;

namespace NFSV2;

/// <summary>
/// Summary description for diropargs.
/// </summary>
public class DiropArgs
{
	FHandle fh;
	string	fileName;

	public DiropArgs(RpcCracker cracker)
	{
		fh			= new FHandle(cracker);
		fileName	= cracker.get_String();
	}

    public FHandle DirHandle => fh;

    public string FileName => fileName;
}
