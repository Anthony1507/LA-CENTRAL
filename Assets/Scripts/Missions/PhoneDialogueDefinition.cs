
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PhoneDialogueLine
{
    public string speaker;

    [TextArea(2, 6)]
    public string text;
}

[CreateAssetMenu(
    fileName = "PhoneDialogue_New",
    menuName = "LA CENTRAL/Phone Dialogue"
)]
public sealed class PhoneDialogueDefinition : ScriptableObject
{
    [Header("Identification")]
    public string incidentId;
    public string callerName;

    [Header("Dialogue")]
    public List<PhoneDialogueLine> lines =
        new List<PhoneDialogueLine>();
}
