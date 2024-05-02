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

namespace PortMapperV1;

/// <summary>
/// Summary description for TCPPortMapper.
/// </summary>
public class PortMapper : RpcD
{
	public PortMapper() : base(Ports.portmapper, Progs.portmapper) { }

	protected override void Proc(uint proc, RpcCracker cracker, RpcPacker packer)
	{
		switch (proc)
		{
			case 3:	// GetPort
				GetPort(cracker, packer);
				break;
			default:
				throw new BadProc();
		}
	}

	private void GetPort(RpcCracker cracker, RpcPacker packer)
	{
		const uint IPPROTO_UDP = 17;

		uint prog = cracker.get_uint32();
		uint vers = cracker.get_uint32();
		uint prot = cracker.get_uint32();
		uint port = cracker.get_uint32();

		Console.WriteLine("prog:{0}, vers:{1}, prot:{2}, port:{3}", prog, vers, prot, port);

		uint registeredPort = 0;

		if (prot == IPPROTO_UDP)
		{
			if (prog == (uint)Progs.mountd && vers == (uint)Vers.mountd)
			{
				registeredPort = (uint)Ports.mountd;
			}
			else if (prog == (uint)Progs.nfsd && vers == (uint)Vers.nfsd)
			{
				registeredPort = (uint)Ports.nfsd;
			}
		}

		packer.SetUint32(registeredPort);
	}
}
