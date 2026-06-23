using UnityEngine;
using TMPro;

public class KeyboardScript : MonoBehaviour
{
    public TMP_InputField TextField;
    public GameObject RusLayoutSml, RusLayoutBig, EngLayoutSml, EngLayoutBig, SymbLayout;

    public void SetTarget(TMP_InputField field)
    {
        TextField = field;
    }

    public void alphabetFunction(string alphabet)
    {
        if (TextField == null) return;
        TextField.text += alphabet;
        TextField.caretPosition = TextField.text.Length;
    }

    public void BackSpace()
    {
        if (TextField == null || TextField.text.Length == 0) return;
        TextField.text = TextField.text.Remove(TextField.text.Length - 1);
        TextField.caretPosition = TextField.text.Length;
    }

    public void CloseAllLayouts()
    {
        RusLayoutSml.SetActive(false);
        RusLayoutBig.SetActive(false);
        EngLayoutSml.SetActive(false);
        EngLayoutBig.SetActive(false);
        SymbLayout.SetActive(false);
    }

    public void ShowLayout(GameObject SetLayout)
    {
        CloseAllLayouts();
        SetLayout.SetActive(true);
    }
}
