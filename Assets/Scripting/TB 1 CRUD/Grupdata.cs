using System;
using System.Collections.Generic;

[Serializable]
public class GrupData
{
    public int id;
    public string kode;                 // kode undangan untuk bergabung
    public string adminId;              // pembuat grup (satu-satunya yang boleh edit dan hapus)
    public List<string> anggotaIds = new List<string>();   // pengguna yang bergabung lewat kode
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