//
// NFS Server
//
// Copyright (c) 2004-2012 Pete Barber
//
// Licensed under the The Code Project Open License (CPOL.html)
// http://www.codeproject.com/info/cpol10.aspx 
//
using System;

namespace RPCV2Lib;

/// <summary>
/// Summary description for rpcCracker.
/// </summary>
public class RpcCracker
{
    private uint i = 0;
    private byte[] data;

    public RpcCracker(byte[] data) => this.data = data;

    public void Jump(uint offset)
    {
        i += offset;
    }

    public uint get_uint32()
    {
        uint value = Convert(data, i);

        i += 4;

        return value;
    }

    public char get_char()
    {
        var value = (char)data[i];

        ++i;

        return value;
    }

    public string get_String()
    {
        uint length = get_uint32();

        var value = "";

        for (int charCount = 0; charCount < length; ++charCount)
            value += get_char();

        if (length % 4 != 0)
            i += (4 - length % 4);

        return value;
    }

    public byte[] getData()
    {
        uint length = get_uint32();

        var buf = new byte[length];

        for (int bufi = 0; bufi < length; ++bufi)
        {
            buf[bufi] = data[i];
            ++i;
        }

        if (length % 4 != 0)
            i += (4 - length % 4);

        return buf;
    }

    static private uint Convert(byte[] data, uint offset)
    {
        uint one = data[offset];
        one <<= 24;
        uint two = data[offset + 1];
        two <<= 16;
        uint three = data[offset + 2];
        three <<= 8;
        uint four = data[offset + 3];

        uint result = one | two | three | four;

        return result;
    }

    public void Dump(string what)
    {
        Dump(what, data, data.Length);
    }

    public static void Dump(string what, Byte[] data, int length)
    {
        Console.WriteLine("{0}, length:{1}", what, length);

        for (uint i = 0; i < length; i += 4)
        {
            Console.WriteLine("{0} {1} {2} {3} {4}: {5}", i, data[i], data[i + 1], data[i + 2], data[i + 3], Convert(data, i));
        }
    }
}

public class RpcPacker
{
    private uint i = 0;
    private byte[] data = new byte[1024];

    public RpcPacker() { }

    public byte[] Data => data;

    public uint Length => i;

    public void SetUint32(uint value)
    {
        Need(4);

        Convert(data, i, value);

        i += 4;
    }

    public void SetString(string value)
    {
        Need(4 + value.Length + 3); // Extra in case of pad

        SetUint32((uint)value.Length);

        //Console.WriteLine("setString length:{0,3}, dump:{1}:{2}:{3}:{4}, name:{5}", value.Length, data[i - 4], data[i - 3], data[i - 2], data[i - 1], value);

        foreach (var c in value)
        {
            data[i] = (byte)c;
            ++i;
        }

        if (value.Length % 4 != 0)
        {
            for (int pad = 4 - value.Length % 4; pad != 0; --pad)
            {
                data[i] = 0;
                ++i;
            }
        }
    }

    static public uint SizeOfString(String value)
    {
        uint size = (uint)value.Length;

        if (size % 4 != 0)
            size += 4 - (size % 4);

        size += 4;

        return size;
    }

    public void SetData(byte[] buf, int length)
    {
        Need(4 + length + 3); // Extra in case of pad

        SetUint32((uint)length);

        for (int bufi = 0; bufi < length; ++bufi)
        {
            data[i] = buf[bufi];
            ++i;
        }

        if (length % 4 != 0)
        {
            for (int pad = 4 - length % 4; pad != 0; --pad)
            {
                data[i] = 0;
                ++i;
            }
        }
    }

    private void Need(int need)
    {
        while (i + need >= data.Length)
        {
            var newData = new byte[data.Length * 2];
            data.CopyTo(newData, 0);

            data = newData;
        }
    }

    protected static void Convert(byte[] data, uint offset, uint value)
    {
        uint one = value;
        uint two = value;
        two >>= 8;
        uint three = value;
        three >>= 16;
        uint four = value;
        four >>= 24;

        data[offset] = (byte)four;
        data[offset + 1] = (byte)three;
        data[offset + 2] = (byte)two;
        data[offset + 3] = (byte)one;
    }

    public void Dump(string what)
    {
        RpcCracker.Dump(what, data, (int)i);
    }
}
