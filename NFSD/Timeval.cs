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
/// Summary description for timeval.
/// </summary>
/// 
public class Timeval
{
	static readonly long UNIXEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0).ToFileTimeUtc();	// Midnight 1/1/1970

	uint seconds	= 0;
	uint useconds	= 0;

	public Timeval()
	{
	}

	public Timeval(System.DateTime when)
	{
		long ticks = when.ToFileTime() - UNIXEpoch;
		ticks /= 10000000;

		seconds = (uint)ticks;
	}

	public Timeval(RpcCracker cracker)
	{
		seconds		= cracker.get_uint32();
		useconds	= cracker.get_uint32();
	}

    // If this was constructed from an RPC message then even though data 
    // was present it maybe invalud, i.e. -1 (well 0xFFFFFFF).
    public bool IsValid() => (int)seconds != -1;

    public System.DateTime When
	{
		get
		{
			long ticks = seconds;
			// TODO: useconds
			ticks *= 10000000;

			ticks += UNIXEpoch;

			return new System.DateTime(ticks);
		}
	}

	public void Pack(RpcPacker packer)
	{
		packer.SetUint32(seconds);
		packer.SetUint32(useconds);
	}
}
