using System;
using System.Collections.Generic;

[Serializable]
public class UserData
{
    public string id;        // email huruf kecil, dipakai sebagai ID unik
    public string email;
    public string nama;      // diambil dari bagian depan email
    public string passHash;  // SHA-256 dari password (bukan password asli)
}

[Serializable]
public class UserList
{
    public List<UserData> items = new List<UserData>();
}
