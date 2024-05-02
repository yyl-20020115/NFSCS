//
// NFS Server
//
// Copyright (c) 2004-2012 Pete Barber
//
// Licensed under the The Code Project Open License (CPOL.html)
// http://www.codeproject.com/info/cpol10.aspx 
//
using System;
using System.IO;
using System.Text;

using RPCV2Lib;

namespace NFSV2;

/// <summary>
/// Summary description for nfsd.
/// </summary>
/// 
public class NFSD : RpcD
{
	private ReadDirRes	results;
	private int			MAXPATHLEN =  1024;

	public NFSD() : base(Ports.nfsd, Progs.nfsd)
	{
	}

    protected override bool Prog(uint prog) => prog == 100227;

    protected override void Proc(uint proc, RpcCracker cracker, RpcPacker packer)
	{
		try
		{
			switch(proc)
			{
				case 1:
					GetAttr(cracker, packer);
					break;
				case 2:
					SetAttr(cracker, packer);
					break;
				case 3:
					// Root(). No-op.
					break;
				case 4:
					Lookup(cracker, packer);
					break;
				case 5:
					ReadLink(cracker, packer);
					break;
				case 6:
					Read(cracker, packer);
					break;
				case 8:
					Write(cracker, packer);
					break;
				case 9:
					Create(cracker, packer);
					break;
				case 10:
					Remove(cracker, packer);
					break;
				case 11:
					Rename(cracker, packer);
					break;
				case 13:
					SymLink(cracker, packer);
					break;
				case 14:
					MkDir(cracker, packer);
					break;
				case 15:
					RmDir(cracker, packer);
					break;
				case 16:
					ReadDir(cracker, packer);
					break;
				case 17:
					StatFS(cracker, packer);
					break;
				default:
					throw new BadProc();
			}
		}
		catch(BadProc)
		{
			throw;
		}
		catch(NFSStatusException e)
		{
			packer.SetUint32((uint)e.Status);
		}
		catch (System.IO.FileNotFoundException)
		{
			packer.SetUint32((uint)NFSStatus.NFSERR_NOENT);
		}
		catch(UnauthorizedAccessException)
		{
			packer.SetUint32((uint)NFSStatus.NFSERR_PERM);
		}
		catch(PathTooLongException)
		{
			packer.SetUint32((uint)NFSStatus.NFSERR_NAMETOOLONG);
		}
		catch(DirectoryNotFoundException)
		{
			packer.SetUint32((uint)NFSStatus.NFSERR_NOTDIR);
		}
		catch(Exception e)
		{
			Console.WriteLine("nfsd error:{0}", e);
			packer.SetUint32((uint)NFSStatus.NFSERR_IO);
		}
	}

	private void GetAttr(RpcCracker cracker, RpcPacker packer)
	{
		ATTRSTAT.PackSuccess(packer, new Fattr(new FHandle(cracker)));
	}

	private void SetAttr(RpcCracker cracker, RpcPacker packer)
	{
		FHandle	fh			= new FHandle(cracker);
		Sattr	attributes	= new Sattr(cracker);

		FileEntry file = FileTable.LookupFileEntry(fh);

		if (file == null)
		{
			Console.WriteLine("Invalid file handle:{0}", fh.Index);
			throw new NFSStatusException(NFSStatus.NFSERR_STALE);
		}

		// TODO: Actually do something with the attributes.
		if (attributes.Size == 0)
		{
			try
			{
				FileStream fs = new FileStream(file.Name, FileMode.Truncate, FileAccess.Write);
				fs.Close();
			}
			catch (System.IO.FileNotFoundException)
			{
				FileTable.Remove(fh);
				throw;
			}
		}

		if ((int)attributes.Mode != -1)
		{
			FileInfo info = new FileInfo(FileTable.LookupFileEntry(fh).Name);

			if ((attributes.Mode & (uint)Modes.WOWN) == (uint)Modes.WOWN)
				info.Attributes &= ~FileAttributes.ReadOnly;
			else
				info.Attributes |= FileAttributes.ReadOnly;
		}

		ATTRSTAT.PackSuccess(packer, new Fattr(fh));
	}

	private void Lookup(RpcCracker cracker, RpcPacker packer)
	{
		DiropArgs args = new DiropArgs(cracker);

		String lookupPath			= FileTable.LookupFileEntry(args.DirHandle).Name + @"\" + args.FileName;
		String symLinkLookupPath	= lookupPath + ".sl";

#if DEBUG
		//Console.WriteLine(@"Lookup: {0}", lookupPath);
#endif

		FHandle fh = null;

#if DEBUG
		try 
		{
#endif
		if ((fh = FileTable.LookupFileHandle(lookupPath)) == null)
		{
			//Console.WriteLine(@"Lookup (symlink): {0}", symLinkLookupPath);

			fh = FileTable.LookupFileHandle(symLinkLookupPath);
		}

		// Entry (for file or symlink) not in FileTable
		if (fh == null)
		{
			// Try non-SL first
			fh = FileTable.Add(new FileEntry(lookupPath));

			try
			{
				DiroPres.PackSuccess(packer, fh, new Fattr(fh));
			}
			catch
			{
				FileTable.Remove(fh);

				fh = FileTable.Add(new FileEntry(symLinkLookupPath));
			}
		}
		// Case where fh is in FileTable and used when neither was but
		// regular file/dir has not been found so add entry for SL
#if DEBUG
		}
		catch
		{
			Console.WriteLine(@"Lookup EXCEPTION: {0}", lookupPath);
			throw;
		}
#endif
		DiroPres.PackSuccess(packer, fh, new Fattr(fh));
	}

	private void ReadLink(RpcCracker cracker, RpcPacker packer)
	{
		FHandle fh = new FHandle(cracker);

		FileStream fs;
		
		try
		{
			fs = new FileStream(FileTable.LookupFileEntry(fh).Name, FileMode.Open, FileAccess.Read);
		}
		catch (System.IO.FileNotFoundException)
		{
			FileTable.Remove(fh);
			throw;
		}

		try
		{
			Byte[] buf = new Byte[MAXPATHLEN];

			int bytesRead = fs.Read(buf, 0, MAXPATHLEN);

			packer.SetUint32((uint)NFSStatus.NFS_OK);
			packer.SetData(buf, buf.Length);
		}
		finally
		{
			fs.Close();
		}

	}

	private void Read(RpcCracker cracker, RpcPacker packer)
	{
		FHandle fh			= new FHandle(cracker);
		uint	offset		= cracker.get_uint32();
		uint	count		= cracker.get_uint32();
		uint	totalCount	= cracker.get_uint32();

		FileStream fs;
		
		try
		{
			fs = new FileStream(FileTable.LookupFileEntry(fh).Name, FileMode.Open, FileAccess.Read);
		}
		catch (System.IO.FileNotFoundException)
		{
			FileTable.Remove(fh);
			throw;
		}

		try
		{
			fs.Position = offset;

			Byte[] buf = new Byte[count];

			int bytesRead = fs.Read(buf, 0, (int)count);

			Fattr attr = new Fattr(fh);

			if (attr.IsFile() == false) throw new NFSStatusException(NFSStatus.NFSERR_ISDIR);

			packer.SetUint32((uint)NFSStatus.NFS_OK);
			attr.Pack(packer);
			packer.SetData(buf, bytesRead);
		}
		finally
		{
			fs.Close();
		}
	}

	private void Write(RpcCracker cracker, RpcPacker packer)
	{
		FHandle	fh			= new FHandle(cracker);
		uint	beginOffset	= cracker.get_uint32();
		uint	offset		= cracker.get_uint32();
		uint	totalcount	= cracker.get_uint32();
		Byte[] data			= cracker.getData();

		FileStream fs;
		
		try
		{
			fs = new FileStream(FileTable.LookupFileEntry(fh).Name, FileMode.Open, FileAccess.Write);
		}
		catch (System.IO.FileNotFoundException)
		{
			FileTable.Remove(fh);
			throw;
		}

		try
		{
			fs.Position = offset;

			fs.Write(data, 0, data.Length);
		
			ATTRSTAT.PackSuccess(packer, new Fattr(fh));
		}
		finally
		{
			fs.Close();
		}
	}

	private void Create(RpcCracker cracker, RpcPacker packer)
	{
		CreateFileOrDirectory(cracker, packer, true);
	}

	private void Remove(RpcCracker cracker, RpcPacker packer)
	{
		DiropArgs args = new DiropArgs(cracker);

		String removePath = FileTable.LookupFileEntry(args.DirHandle).Name + @"\" + args.FileName;

		FileInfo info = new FileInfo(removePath);

		if (info.Exists == false)
		{
			removePath += ".sl";
			info = new FileInfo(removePath);
		}

		Console.WriteLine(@"Remove: {0}", removePath);

		FHandle fh = FileTable.LookupFileHandle(removePath);

		info.Delete();
		// If UnauthorizedAccessException is thrown & caught should 
		// probably stat file to determine if the cause is because
		// the path is a dir rather than a directory.

		if (fh != null) FileTable.Remove(fh);

		packer.SetUint32((uint)NFSStatus.NFS_OK);
	}

	private void Rename(RpcCracker cracker, RpcPacker packer)
	{
		DiropArgs from	= new DiropArgs(cracker);
		DiropArgs to	= new DiropArgs(cracker);

		string fromPath = FileTable.LookupFileEntry(from.DirHandle).Name + @"\" + from.FileName; 
		string toPath = FileTable.LookupFileEntry(to.DirHandle).Name + @"\" + to.FileName; 

		Console.WriteLine("Rename {0} to {1}", fromPath, toPath);

		if (File.Exists(toPath) == true)
			File.Delete(toPath);

		File.Move(fromPath, toPath);

		// Only bother updating the FileTable if the operation was successful
		FileTable.Rename(fromPath, toPath);

		packer.SetUint32((uint)NFSStatus.NFS_OK);
	}

	private void SymLink(RpcCracker cracker, RpcPacker packer)
	{
		DiropArgs	args	= new DiropArgs(cracker);
		string		path	= cracker.get_String();
		Sattr		attr	= new Sattr(cracker);

		String createPath = FileTable.LookupFileEntry(args.DirHandle).Name + @"\" + args.FileName + ".sl";

		Console.WriteLine("Symlink: {0}->{1}", createPath, path);

		FHandle fh;

		if ((fh = FileTable.LookupFileHandle(createPath)) == null)
			fh = FileTable.Add(new FileEntry(createPath));

		try
		{
			FileStream symlink = new FileStream(createPath, FileMode.CreateNew, FileAccess.Write);

			try
			{
				UTF8Encoding pathUTF8 = new UTF8Encoding();

				byte[] buf = pathUTF8.GetBytes(path);

				symlink.Write(buf, 0, buf.Length);

				packer.SetUint32((uint)NFSStatus.NFS_OK);
			}
			finally
			{
				symlink.Close();
			}
		}
		catch(IOException)
		{
			if (new FileInfo(createPath).Exists == true)
				throw new NFSStatusException(NFSStatus.NFSERR_EXIST);
			else
				throw;
		}
	}

	private void MkDir(RpcCracker cracker, RpcPacker packer)
	{
		CreateFileOrDirectory(cracker, packer, false);
	}

	private void RmDir(RpcCracker cracker, RpcPacker packer)
	{
		DiropArgs args = new DiropArgs(cracker);

		String removePath = FileTable.LookupFileEntry(args.DirHandle).Name + @"\" + args.FileName;

		Console.WriteLine(@"RmDir: {0}", removePath);

		FHandle fh = FileTable.LookupFileHandle(removePath);

		try
		{
			new DirectoryInfo(removePath).Delete(false);
		}
		catch (IOException)
		{
			if (new DirectoryInfo(removePath).GetFileSystemInfos().Length > 0)
				throw new NFSStatusException(NFSStatus.NFSERR_NOTEMPTY);
			else
					throw new NFSStatusException(NFSStatus.NFSERR_PERM);
		}

		if (fh != null) FileTable.Remove(fh);

		packer.SetUint32((uint)NFSStatus.NFS_OK);
	}

	private void ReadDir(RpcCracker cracker, RpcPacker packer)
	{
		FHandle fh	= new FHandle(cracker);
		uint cookie	= cracker.get_uint32();
		uint count	= cracker.get_uint32();

		FileEntry dir = FileTable.LookupFileEntry(fh);

		//Console.WriteLine("ReadDir:{0}, cookie:{1}, count:{2}, resultsNULL:{3}", dir.Name, cookie, count, results == null);

		if (cookie == 0 || results == null)
		{
			if (dir == null) throw new NFSStatusException(NFSStatus.NFSERR_EXIST);

			try
			{
				results = new ReadDirRes(dir.Name, count);
			}
			catch(DirectoryNotFoundException)
			{
				FileTable.Remove(fh);
				throw;
			}
		}

		if (results.Pack(packer, cookie, count) == true)
			results = null;
	}

	private void StatFS(RpcCracker cracker, RpcPacker packer)
	{
		const uint BLOCK_SIZE = 4096;

		FHandle fh = new FHandle(cracker);

		FileEntry file = FileTable.LookupFileEntry(fh);

		Console.WriteLine("StatFS: {0}", file.Name);

		System.UInt64 freeBytesAvailable		= 0;
		System.UInt64 totalNumberOfBytes		= 0;
		System.UInt64 totalNumberOfFreeBytes	= 0;

		if (UnmanagedWin32API.GetDiskFreeSpaceEx(file.Name, ref freeBytesAvailable, ref totalNumberOfBytes, ref totalNumberOfFreeBytes) == false)
			throw new NFSStatusException(NFSStatus.NFSERR_EXIST);

		freeBytesAvailable		/= BLOCK_SIZE;
		totalNumberOfBytes		/= BLOCK_SIZE;
		totalNumberOfFreeBytes	/= BLOCK_SIZE;

		packer.SetUint32((uint)NFSStatus.NFS_OK);
		packer.SetUint32(BLOCK_SIZE);				// tsize: optimum transfer size
		packer.SetUint32(BLOCK_SIZE);				// Block size of FS
		packer.SetUint32((uint)totalNumberOfBytes);		// Total # of blocks (of the above size)
		packer.SetUint32((uint)totalNumberOfFreeBytes);	// Free blocks
		packer.SetUint32((uint)freeBytesAvailable);		// Free blocks available to non-priv. users
	}

	private void CreateFileOrDirectory(RpcCracker cracker, RpcPacker packer, bool createFile)
	{
		CreateArgs args = new CreateArgs(cracker);

		String createPath = FileTable.LookupFileEntry(args.Where.DirHandle).Name + @"\" + args.Where.FileName;

		Console.WriteLine("Create: {0}", createPath);

		FHandle fh;

		if ((fh = FileTable.LookupFileHandle(createPath)) == null)
			fh = FileTable.Add(new FileEntry(createPath));

		if (createFile == true)
			new FileInfo(createPath).Create().Close();
		else
			new DirectoryInfo(createPath).Create();

		var attr = new Fattr(fh);

		if (attr.IsFile() != createFile)
			throw new System.Exception();

		DiroPres.PackSuccess(packer, fh, attr);
	}
}
