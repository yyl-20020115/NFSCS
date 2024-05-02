//
// NFS Server
//
// Copyright (c) 2004-2012 Pete Barber
//
// Licensed under the The Code Project Open License (CPOL.html)
// http://www.codeproject.com/info/cpol10.aspx 
//
using System;
using System.Collections;

namespace RPCV2Lib;

public class HandleTable
{
	private object[]	objects;
	private Stack		free = new ();
	private uint		next = 1;

	public HandleTable(int size)
	{
		objects = new object[size];
	}

    public Object this[uint i] => objects[i];

    public uint Add(object obj)
	{
		uint i;

		if (free.Count > 0)
		{
			i = (uint)free.Pop();
		}
		else
		{
			if (next == objects.Length)
			{
				var newObjects = new object[next * 2];

				objects.CopyTo(newObjects, 0);

				objects = newObjects;
			}

			i = next;
			++next;
		}

		objects[i] = obj;

#if DEBUG
		Console.WriteLine("HandleTable.Add:{0}", i);
#endif

		return (uint)i;
	}

	public void Remove(uint fh)
	{
#if DEBUG
		Console.WriteLine("HandleTable.Remove:{0}", fh);
#endif
		objects[fh] = null;
		free.Push(fh);
	}

    public uint Length => next;

}

public class FHandle
{
	private uint index;

    public FHandle(uint index) => this.index = index;

    public FHandle(RpcCracker cracker)
	{
		index	= cracker.get_uint32();

		cracker.get_uint32();
		cracker.get_uint32();
		cracker.get_uint32();
		cracker.get_uint32();
		cracker.get_uint32();
		cracker.get_uint32();
		cracker.get_uint32();
	}

	public void Pack(RpcPacker packer)
	{
		packer.SetUint32(index);

		// Pad
		packer.SetUint32(0);
		packer.SetUint32(0);
		packer.SetUint32(0);
		packer.SetUint32(0);
		packer.SetUint32(0);
		packer.SetUint32(0);
		packer.SetUint32(0);
	}

    public uint Index => index;
}

public class FileEntry(string name)
{
	private string name = name;

    public string Name
    {
        get => name;
        set => name = value;
    }
}

public class FileTable
{
	static private HandleTable files;

	public FileTable(int size)
	{
		files = new HandleTable(size);
	}

	public static FileEntry LookupFileEntry(FHandle fh)
	{
		try
		{
			return (FileEntry)files[fh.Index];
		}
		catch (IndexOutOfRangeException)
		{
#if DEBUG
			Console.WriteLine("LookupFileEntry({0}) failed", fh.Index);
#endif
			return null;
		}
	}

	public static FHandle LookupFileHandle(string name)
	{
		for (uint i = 0; i < files.Length; ++i)
		{
			object o = files[i];

			if (o != null && name == ((FileEntry)o).Name)
				return new FHandle(i);
		}

#if DEBUG
		Console.WriteLine("LookupFileHandle({0}) failed", name);
#endif

		return null;
	}

    public static FHandle Add(FileEntry file) => new (files.Add(file));

    public static void Rename(string from, string to)
	{
		var fhFrom	= LookupFileHandle(from);
		var fhTo	= LookupFileHandle(to);

		if (fhFrom != null && fhTo == null)
		{
			// Most likely
			LookupFileEntry(fhFrom).Name = to;
		}
		else if (fhFrom != null && fhTo != null)
		{
			// Next most likely
			Remove(fhTo);
			LookupFileEntry(fhFrom).Name = to;
		}
		if (fhFrom == null && fhTo != null)
		{
			// Nothing to do
		}
		else if (fhFrom == null && fhTo == null)
		{
			Add(new FileEntry(to));
		}
	}

	public static void Remove(FHandle fh)
	{
		files.Remove(fh.Index);
	}
}
