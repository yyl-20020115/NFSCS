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

/// <summary>
/// Summary description for readdirres.
/// </summary>
public class Entry
{
	private uint	fileid;
	private string	name;
	private uint	cookie;

	// Special constructor for "." and ".."
	public Entry(string name, string fullName, uint cookie, uint fileid)
	{
		this.fileid = fileid;
		this.name	= name;
		this.cookie	= cookie;

		//Console.WriteLine("entry fileid:{0,5}, cookie:{1,5}, name:{2}", fileid, cookie, name);
	}

	public Entry(FileSystemInfo info, uint cookie)
	{
		this.name = info.Name;

		if (info.Attributes != FileAttributes.Directory && 
			info.Attributes != FileAttributes.Device	&& 
			info.Extension == ".sl")
			this.name = this.name.Remove(this.name.Length - 3, 3);

		this.cookie	= cookie;

		FHandle fh;

		if ((fh = FileTable.LookupFileHandle(info.FullName)) == null)
			fh = FileTable.Add(new FileEntry(info.FullName));

		this.fileid = fh.Index;

		//Console.WriteLine("entry fileid:{0,5}, cookie:{1,5}, name:{2}", fileid, cookie, name);
	}

	public void Pack(RpcPacker packer)
	{
		//Console.WriteLine("entry pack name:{0}", name);

		packer.SetUint32(fileid);
		packer.SetString(name);
		packer.SetUint32(cookie);
	}

    public uint Size => 12 + RpcPacker.SizeOfString(name);
}

public class ReadDirRes
{
	Entry[] entries;

	public ReadDirRes(string dirName, uint count)
	{
		DirectoryInfo dir = new DirectoryInfo(dirName);

		FileSystemInfo[] files = dir.GetFileSystemInfos();

		entries = new Entry[files.Length + 2];


		// Don't create new entries in FileTable for "." and "..".  Find the real dirs. and use those id's
		uint dirFileId = FileTable.LookupFileHandle(dirName).Index;

		entries[0] = new Entry(".", dirName + @"\.", 1, dirFileId);

		if (dirFileId == 1) // root
			entries[1] = new Entry("..", dirName + @"\..", files.Length == 0 ? count : 2, 1);
		else
			entries[1] = new Entry("..", dirName + @"\..", files.Length == 0 ? count : 2, FileTable.LookupFileHandle(dir.Parent.FullName).Index);


		uint i = 2;

		foreach (FileSystemInfo file in files)
			if (files.Length == i - 1)
				entries[i] = new Entry(file, count);
			else
				entries[i] = new Entry(file, ++i);
	}

	public bool Pack(RpcPacker packer, uint cookie, uint count)
	{

		packer.SetUint32((uint)NFSStatus.NFS_OK);

      		uint size = 8;	// First pointer + EOF

		if (cookie >= entries.Length)
		{
			// nothing
		}
		else
		{
			do
			{
				var next = entries[cookie];

				if (size + next.Size > count)
					break;
				else
					size += next.Size;

				// true as in yes, more follows.  This is *entry.
				packer.SetUint32(1);

				next.Pack(packer);
			}
			while (++cookie < entries.Length);
		}

		// false as in no more follow.  This is *entry.
		// Unlike EOF which is set only when all entries have been sent
		// *entry is reset to false following the last entry in each
		// batch.
		packer.SetUint32(0);

		//Console.WriteLine("ReadDir: Pack done.  cookie:{0}, size:{1}", cookie, size);

		// EOF
		if (cookie >= entries.Length)
		{
			packer.SetUint32((uint)1);	// yes
			return true;
		}
		else
		{
			packer.SetUint32((uint)0);	// no
			return false;
		}
	}
}
