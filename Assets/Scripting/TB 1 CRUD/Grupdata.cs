using System;
using System.Collections.Generic;

// Satu grup Tabungan Bersama. Disimpan di Firebase Realtime Database pada grup/{id}.
// Objek ini hanya "salinan baca" untuk tampilan; GrupService yang mengisi dan menulisnya.
[Serializable]
public class GrupData
{
    public string id;                   // kunci grup di Firebase (dibuat otomatis)
    public string kode;                 // kode undangan untuk bergabung
    public string adminId;              // email pembuat grup (satu-satunya yang boleh edit, hapus, konfirmasi)
    public List<string> anggotaIds = new List<string>();   // email anggota yang bergabung lewat kode (tanpa admin)
    public string nama;
    public int jumlahAnggota;           // dipakai tampilan: paling sedikit sebanyak anggota yang benar-benar ada
    public long target;
    public long terkumpul;              // jumlah setoran yang sudah dikonfirmasi (dihitung, tidak disimpan)
    public string tanggal;              // deadline (teks bebas)
    public List<SetoranData> setoran = new List<SetoranData>();
}