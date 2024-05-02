//
// NFS Server
//
// Copyright (c) 2004-2012 Pete Barber
//
// Licensed under the The Code Project Open License (CPOL.html)
// http://www.codeproject.com/info/cpol10.aspx 
//
using System;
using System.Threading;

using RPCV2Lib;
using PortMapperV1;
using MountV1;
using NFSV2;

namespace NFS;

/// <summary>
/// Summary description for Class1.
/// </summary>
public static class NFS
{
	/// <summary>
	/// The main entry point for the application.
	/// </summary>
	[STAThread]
	public static void Main(string[] args)
	{
		FileTable fileHandles = new FileTable(1024);

		var portMapper = new Thread(new ThreadStart(new PortMapper().Run));
		var mountD = new Thread(new ThreadStart(new MountD().Run));
		var nfsD = new Thread(new ThreadStart(new NFSD().Run));

		portMapper.Start();
		mountD.Start();
		nfsD.Start();

		nfsD.Join();
		mountD.Join();
		portMapper.Join();
	}
}
