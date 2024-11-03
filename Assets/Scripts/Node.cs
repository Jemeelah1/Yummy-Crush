using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    // Determine if our game needs space
    public bool isUsable;
    public GameObject portion;

    public Node(bool _isUsable, GameObject _portion)
    {
        isUsable = _isUsable;
        portion = _portion;
    }
}
