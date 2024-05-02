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
/// Summary description for diropres.
/// </summary>
public class DiroPres
{
	static public void PackSuccess(RpcPacker packer, FHandle fh, Fattr attr)
	{
		packer.SetUint32((uint)NFSStatus.NFS_OK);
		fh.Pack(packer);
		attr.Pack(packer);
	}

	static public void PackError(RpcPacker packer, NFSStatus error)
	{
		packer.SetUint32((uint)error);
	}
}
