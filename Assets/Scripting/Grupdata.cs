using System;
using System.Collections.Generic;

[Serializable]
public class GrupData
{
    public int id;
    public string nama;
    public int jumlahAnggota;
    public long target;
    public long terkumpul;
    public string tanggal;
}

// JsonUtility tidak bisa serialisasi List langsung, jadi dibungkus class ini
[Serializable]
public class GrupList
{
    public List<GrupData> items = new List<GrupData>();
}