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
using RPCV2Lib;

namespace NFSV2;
public enum FType : uint
{
    NFNON = 0,
    NFREG = 1,
    NFDIR = 2,
    NFBLK = 3,
    NFCHR = 4,
    NFLNK = 5
}
[Flags]
public enum Modes : uint
{
    DIR = 16384,
    CHR = 8192,
    BLK = 24576,
    REG = 32768,
    LNK = 40960,
    NON = 49152,
    SUID = 2048,
    SGID = 1024,
    SWAP = 512,
    ROWN = 256,
    WOWN = 128,
    XOWN = 64,
    RGRP = 32,
    WGRP = 16,
    XGRP = 8,
    ROTH = 4,
    WOTH = 2,
    XOTH = 1
}
/// <summary>
/// Summary description for fattr.
/// </summary>
public class Fattr
{

	FType	type;
	uint	mode		= (uint)(Modes.XOWN | Modes.XGRP | Modes.XOTH | Modes.ROWN | Modes.RGRP | Modes.ROTH);
	uint	nlink		= 1;
	uint	uid			= 0;
	uint	gid			= 0;
	uint	size		= 0;
	uint	blocksize	= 0;
	uint	rdev		= 0;
	uint	blocks		= 0;
	uint	fsid		= 1;
	uint	fileid		= 0;
	Timeval	atime;
	Timeval mtime;

	public Fattr(FHandle fh)
	{
		FileEntry file = FileTable.LookupFileEntry(fh);

		if (file == null)
		{
			Console.WriteLine("fattr on invalid file handle:{0}", fh.Index);
			throw new NFSStatusException(NFSStatus.NFSERR_STALE);
		}

		FileInfo fileInfo = new FileInfo(file.Name);

		if (fileInfo.Exists == false)
			if (new DirectoryInfo(file.Name).Exists == false)
				throw new System.IO.FileNotFoundException();

		if ((fileInfo.Attributes & FileAttributes.Directory) != 0)
		{
			type = FType.NFDIR;
			mode |= (uint)Modes.DIR;
			size = 4096;
			blocksize = 4096;
			blocks = 8;
			atime = new Timeval(fileInfo.LastAccessTime);
			mtime = new Timeval(fileInfo.LastWriteTime);
		}
		else
		{
			if (fileInfo.Extension == ".sl")
			{
				type |= FType.NFLNK;
				mode = (uint)Modes.LNK;
			}
			else
			{
				type = FType.NFREG;
				mode |= (uint)Modes.REG;
			}

			size	= (uint)fileInfo.Length;
			blocks	= (size / 4096) + (4096 - (size % 4096));
			atime	= new Timeval(fileInfo.LastAccessTime);
			mtime	= new Timeval(fileInfo.LastWriteTime);
		}

		if ((fileInfo.Attributes & FileAttributes.ReadOnly) == 0)
			mode |= (uint)Modes.WOWN;

		fileid = fh.Index;

		//Console.WriteLine("fattr name:{0}, fileid:{1}, attrs:{2}, readonly:{3}", file.Name, fileid, fileInfo.Attributes, (fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
	}

    public bool IsFile() => type != FType.NFDIR;

    public void Pack(RpcPacker packer)
	{
		packer.SetUint32((uint)type);
		packer.SetUint32(mode);
		packer.SetUint32(nlink);
		packer.SetUint32(uid);
		packer.SetUint32(gid);
		packer.SetUint32(size);
		packer.SetUint32(blocksize);
		packer.SetUint32(rdev);
		packer.SetUint32(blocks);
		packer.SetUint32(fsid);
		packer.SetUint32(fileid);
		atime.Pack(packer);
		mtime.Pack(packer);
		mtime.Pack(packer);
	}
}
