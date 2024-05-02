//
// NFS Server
//
// Copyright (c) 2004-2012 Pete Barber
//
// Licensed under the The Code Project Open License (CPOL.html)
// http://www.codeproject.com/info/cpol10.aspx 
//
namespace NFSV2;

/// <summary>
/// Summary description for NFSStatusException.
/// </summary>
public class NFSStatusException : System.ApplicationException
{
    private readonly NFSStatus status;

    public NFSStatusException(NFSStatus status) => this.status = status;

    public NFSStatus Status => status;
}
