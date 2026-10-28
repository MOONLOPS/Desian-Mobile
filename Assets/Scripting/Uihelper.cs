using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Helper agar script bisa dipakai dengan UI legacy (Text/InputField) maupun TextMeshPro.
// Cukup drag GameObject-nya ke Inspector, tipe komponen dicek otomatis.
public static class UIHelper
{
    public static void SetText(GameObject go, string value)
    {
        if (go == null) return;
        var tmp = go.GetComponent<TMP_Text>();
        if (tmp != null) { tmp.text = value; return; }
        var legacy = go.GetComponent<Text>();
        if (legacy != null) { legacy.text = value; return; }

        var tmpInput = go.GetComponent<TMP_InputField>();
        if (tmpInput != null) { tmpInput.text = value; return; }
        var legacyInput = go.GetComponent<InputField>();
        if (legacyInput != null) { legacyInput.text = value; return; }

        Debug.LogWarning($"UIHelper: {go.name} tidak punya komponen teks/input.", go);
    }

    public static string GetText(GameObject go)
    {
        if (go == null) return "";
        var tmpInput = go.GetComponent<TMP_InputField>();
        if (tmpInput != null) return tmpInput.text;
        var legacyInput = go.GetComponent<InputField>();
        if (legacyInput != null) return legacyInput.text;
        var tmp = go.GetComponent<TMP_Text>();
        if (tmp != null) return tmp.text;
        var legacy = go.GetComponent<Text>();
        if (legacy != null) return legacy.text;
        return "";
    }

    public static void SetNumberOnly(GameObject go)
    {
        if (go == null) return;
        var tmpInput = go.GetComponent<TMP_InputField>();
        if (tmpInput != null) { tmpInput.contentType = TMP_InputField.ContentType.IntegerNumber; return; }
        var legacyInput = go.GetComponent<InputField>();
        if (legacyInput != null) legacyInput.contentType = InputField.ContentType.IntegerNumber;
    }
}