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
/// Summary description for createargs.
/// </summary>
public class CreateArgs
{
    DiropArgs where;
    Sattr attributes;

    public CreateArgs(RpcCracker cracker)
    {
        where = new DiropArgs(cracker);
        attributes = new Sattr(cracker);
    }

    public DiropArgs Where => where;

    public Sattr Attributes => attributes;
}
