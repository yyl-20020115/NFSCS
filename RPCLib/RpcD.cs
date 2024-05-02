//
// NFS Server
//
// Copyright (c) 2004-2012 Pete Barber
//
// Licensed under the The Code Project Open License (CPOL.html)
// http://www.codeproject.com/info/cpol10.aspx 
//
using System;
using System.Net;
using System.Net.Sockets;

namespace RPCV2Lib;

/// <summary>
/// Summary description for Class1.
/// </summary>
public enum Progs { portmapper = 100000, mountd = 100005, nfsd = 100003 }
public enum Ports { portmapper = 111, mountd = 635, nfsd = 2049 }
public enum Vers { portmapper = 2, mountd = 1, nfsd = 2 }

public abstract class RpcD
{
	private UdpClient	Conn;
	IPEndPoint			RemoteHost;
	private uint		_Prog;

	static uint			Count = 0;

	protected abstract void Proc(uint proc, RpcCracker cracker, RpcPacker reply);

    // Apparently some RPC servers pretend to be multiple server.
    // The case that prompted this was nfs which is also nfs_acl well
    // at least when mounted from a Solaris machine.  This virtual
    // function allows a test for additional RPC prog. numbers to be
    // performed.
    protected virtual bool Prog(uint prog) => false;

    public RpcD(Ports portNumber, Progs prog)
	{
		this._Prog = (uint)prog;

		Conn = new UdpClient(new IPEndPoint(IPAddress.Any, (int)portNumber));
	}

	public void Run()
	{
		while (true)
		{
			RemoteHost = new IPEndPoint(IPAddress.Any, 0);

			var data = Conn.Receive(ref RemoteHost);

			Console.WriteLine("{0}: Received a connection from:{1}", _Prog, RemoteHost.ToString());

			RpcCracker cracker = new RpcCracker(data);
			
			//cracker.Dump("Received");

			RpcPacker reply = CrackRPC(cracker);

			Console.WriteLine("{0}: Sending a reply to:{1}", _Prog, RemoteHost.ToString());

			//reply.Dump("Sending");

			int sent = Conn.Send(reply.Data, (int)reply.Length, RemoteHost);

			if (sent != (int)reply.Length)
				Console.WriteLine("*** Didn't send all.  Length:{0}, sent:{1}", reply.Length, sent);
		}
	}

	private RpcPacker CrackRPC(RpcCracker cracker)
	{
		uint xid		= cracker.get_uint32();
		uint msg_type	= cracker.get_uint32();
		uint rpcvers	= cracker.get_uint32();
		uint prog		= cracker.get_uint32();
		uint vers		= cracker.get_uint32();
		uint proc		= cracker.get_uint32();

#if DEBUG
		Console.WriteLine("{0}> {1}: xid:{2}, type:{3}, rpcvers:{4}, prog:{5}, vers:{6}, proc:{7}", ++Count, this._Prog, xid, msg_type, rpcvers, prog, vers, proc);
#endif

		if (msg_type != 0)
			return GarbageArgsReply(xid);

		if (rpcvers != 2)
			return RPCMismatchReply(xid);

		if (this._Prog != prog && Prog(prog) != true)
			return ProgMismatchReply(xid);

		CrackCredentials(cracker);
		CrackVerifier(cracker);

		try
		{
			var reply = SuccessReply(xid);

			if (proc != 0)
				Proc(proc, cracker, reply);

			return reply;
		}
		catch (Exception e)
		{
			System.Console.WriteLine("Whoops: {0}", e);
			return ProcUnavilReply(xid);
		}
	}

	private void CrackCredentials(RpcCracker cracker)
	{
		var flavor = cracker.get_uint32();
		var length = cracker.get_uint32();

		//Console.WriteLine("{0}: Credentials.  flavor:{1}, length:{2}", prog, flavor, length);

		cracker.Jump(length);
	}

	private void CrackVerifier(RpcCracker cracker)
	{
		var flavor = cracker.get_uint32();
		var length = cracker.get_uint32();

		//Console.WriteLine("{0}: Credentials.  flavor:{1}, length:{2}", prog, flavor, length);

		cracker.Jump(length);
	}

	private RpcPacker NewAcceptReply(uint xid, uint acceptStatus)
	{
		var reply = new RpcPacker();

		reply.SetUint32(xid);
		reply.SetUint32(1);		// rpc_msg.REPLY
		reply.SetUint32(0);		// rpc_msg.reply_body.MSG_ACCEPTED
		reply.SetUint32(0);		// rpc_msg.reply_body.accepted_reply.opaque_auth.NULL
		reply.SetUint32(0);		// rpc_msg.reply_body.accepted_reply.opaque_auth.<datsize>

		// rpc_msg.reply_body.accepted_reply.<case>
		reply.SetUint32(acceptStatus);		

		return reply;
	}

	private RpcPacker SuccessReply(uint xid) => NewAcceptReply(xid, 0);

	private RpcPacker ProgMismatchReply(uint xid)
	{
		var reply = NewAcceptReply(xid, 2);

		reply.SetUint32(_Prog);	// rpc_msg.reply_body.accepted_reply.mismatch_info.low
		reply.SetUint32(_Prog);	// rpc_msg.reply_body.accepted_reply.mismatch_info.high

		return reply;
	}

    private RpcPacker ProcUnavilReply(uint xid) => NewAcceptReply(xid, 3);

    private RpcPacker GarbageArgsReply(uint xid) => NewAcceptReply(xid, 4);

    private RpcPacker RPCMismatchReply(uint xid)
	{
		var reply = new RpcPacker();

		reply.SetUint32(xid);
		reply.SetUint32(1);		// rpc_msg.REPLY
		reply.SetUint32(1);		// rpc_msg.reply_body.MSG_DENIED
		reply.SetUint32(0);		// rpc_msg.reply_body.rejected_reply.RPC_MISMATCH
		reply.SetUint32(2);		// rpc_msg.reply_body.rejected_reply.mismatch_info.low
		reply.SetUint32(2);		// rpc_msg.reply_body.rejected_reply.mismatch_info.low

		return reply;
	}

}

public class BadProc : System.ApplicationException
{
}
