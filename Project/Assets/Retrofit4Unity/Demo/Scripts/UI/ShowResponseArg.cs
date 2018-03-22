using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ShowResponseArg : MonoBehaviour
{
    public Text argName;

    public InputField argValue;

    public void ShowArg(string name, string value)
    {
        if (argName)
        {
            argName.text = name;
        }
        if (argValue)
        {
            if (value.Length > 1024)
            {
                argValue.text = value.Substring(0,1024);
                return;
            }
            argValue.text = value;
        }
    }

    public void Reset()
    {
        if (argName)
        {
            argName.text = string.Empty;
        }
        if (argValue)
        {
            argValue.text = string.Empty;
        }
    }
}
