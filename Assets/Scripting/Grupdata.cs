using System;
using System.Collections.Generic;

[Serializable]
public class GrupData
{
    public int id;
    public string kode;          // kode unik untuk bergabung ke grup
    public string nama;
    public int jumlahAnggota;
    public long target;
    public long terkumpul;
    public string tanggal;
}

[Serializable]
public class GrupList
{
    public List<GrupData> items = new List<GrupData>();
}