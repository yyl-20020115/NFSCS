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
/// Summary description for attrstat.
/// </summary>
public class ATTRSTAT
{
    static public void PackSuccess(RpcPacker packer, Fattr attr)
    {
        packer.SetUint32((uint)NFSStatus.NFS_OK);
        attr.Pack(packer);
    }

    static public void PackError(RpcPacker packer, NFSStatus error)
    {
        packer.SetUint32((uint)error);
    }
}
