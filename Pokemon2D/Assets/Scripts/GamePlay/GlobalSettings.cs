using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalSettings : MonoBehaviour
{
    [SerializeField] Color highlighteColor;

    public Color HighlightedColor => highlighteColor;
    public static GlobalSettings i { get; private set; }
    private void Awake()
    {
        i = this;   
    }
}
