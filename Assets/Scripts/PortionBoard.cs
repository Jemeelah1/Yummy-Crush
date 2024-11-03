using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortionBoard : MonoBehaviour
{
    // define size of the board
    public int width  = 6;
    public int height = 8;

    public Portion[,] portions;

    // define some spacing for the board
    public float spacingX;
    public float spacingY;
    
    // get a reference to our portion prefabs
    public GameObject[] portionPrefabs;

    // get a reference to the collection nodes portionBoard + GO
    public Node[,] portionBoard;
    public GameObject portionBoardGO;
    private GameObject[,] board; // Example 2D array
    private int boardWidth;
    private int boardHeight;


    public List<GameObject> portionsToDestroy = new();
    public GameObject portionParent;

    [SerializeField]
    private Portion selectedPortion;
    
    [SerializeField]
    private bool isProcessingMove;

    // layoutArray
    public ArrayLayout arrayLayout;

    //public static of portionBoard
    public static PortionBoard Instance;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitializeBoard();
        boardWidth = board.GetLength(0); // Rows
        boardHeight = board.GetLength(1); // Columns
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            if (hit.collider != null && hit.collider.gameObject.GetComponent<Portion>())
            {
                if (isProcessingMove)
                   return;
                Portion _portion = hit.collider.gameObject.GetComponent<Portion>();

                SelectPortion(_portion);
            }
        }
    }

    void InitializeBoard()
    {
        DestroyPortions();
        board = new GameObject[boardWidth, boardHeight];

        portionBoard = new Node[width, height];

        spacingX = (float)(width - 1) / 2;
        spacingY = ((float)(height - 1) / 2) + 1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new Vector2(x - spacingX, y - spacingY);
                if (arrayLayout.rows[y].row[x])
                {
                    portionBoard[x, y] = new Node(false, null);
                }
                else
                {
                    int randomIndex = Random.Range(0, portionPrefabs.Length);

                    GameObject portion = Instantiate(portionPrefabs[randomIndex], position, Quaternion.identity);
                    portion.transform.SetParent(portionParent.transform);
                    portion.GetComponent<Portion>().SetIndicies(x, y);
                    portionBoard[x, y] = new Node(true, portion);
                    portionsToDestroy.Add(portion);
                }

            }
        }
        if (CheckBoard(true))
        {
            Debug.Log("we have Matches, lets recreate the board");
            InitializeBoard();
        }
    }

    private void DestroyPortions()
    {
        if (portionsToDestroy != null)
        {
            foreach (GameObject portion in portionsToDestroy)
            {
                Destroy(portion);
            }
            portionsToDestroy.Clear();
        }
    }

    public bool CheckBoard(bool _takeAction)
    {
        bool hasMatched = false;
        List<Portion> portionToRemove = new List<Portion>();

        // Reset all portions to not matched
        foreach (Node nodePortion in portionBoard)
        {
            if (nodePortion.portion != null && nodePortion.portion != null) // Ensure nodePortion.portion is not null
            {
                nodePortion.portion.GetComponent<Portion>().isMatched = false;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Checking if portion node is usable
                if (portionBoard[x, y]?.isUsable == true && portionBoard[x, y]?.portion != null)
                {
                    // Get portion class in node
                    Portion portion = portionBoard[x, y].portion.GetComponent<Portion>();

                    // Ensure it's not matched
                    if (!portion.isMatched)
                    {
                        // Run some matching logic
                        MatchResult matchedPortions = IsConnected(portion);

                        if (matchedPortions.connectedPortions.Count >= 3)
                        {
                            MatchResult superMatchedPortions = SuperMatch(matchedPortions);
                            // Complex matching...
                            portionToRemove.AddRange(superMatchedPortions.connectedPortions);

                            foreach (Portion pot in superMatchedPortions.connectedPortions)
                            {
                                pot.isMatched = true;
                            }
                            
                            hasMatched = true;
                        }
                    }
                }
            }
        }

        if (_takeAction)
        {
            foreach (Portion portionsToRemove in portionToRemove) // Use a different name here
            {
                if (portionsToRemove != null) // Check for null
                {
                    portionsToRemove.isMatched = false; // Safely set isMatched
                }
            }
            
            RemoveAndRefill(portionToRemove); 

            // Recursive check for more matches
            if (CheckBoard(false))
            {
                CheckBoard(true);
            }
        }

        return hasMatched;
    }

    #region Cascading Portions
    //RemoveAndRefill (List of portions)
    private void RemoveAndRefill(List<Portion> _portionsToRemove)
    {
        //remove portions and clearing the board at that location
        foreach (Portion portion in _portionsToRemove)
        {
            //getting its x and y indicies and storing them
            int x = portion.xIndex;
            int y = portion.yIndex;

            //Destroy the portion
            Destroy(portion.gameObject);

            //Create a blank node on the portion board
            portionBoard[x, y] = new Node(true, null);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (portionBoard[x, y].portion == null)
                {
                    Debug.Log("The location X: " + x + " Y: " + y + " is empty, attempting to refill it.");
                    RefillPortion(x, y);
                }
            }
        }
    }

    //RefillPortions
    private void RefillPortion(int x, int y)
    {
        //y offset
        int yOffset = 1;

        //while the cell above our current cell is null and we're below the height of the board
        while (y + yOffset < height && portionBoard[x, y + yOffset].portion == null)
        {
            //increment yOffset
            yOffset++;
        }
        if (y + yOffset < height)
        {
            if (portionBoard[x, y + yOffset] != null && portionBoard[x, y + yOffset].portion != null)
            {
                PortionBoard portionBoardInstance = FindObjectOfType<PortionBoard>();
                Portion portionAbove = portionBoardInstance.portions[x, y + yOffset]?.GetComponent<Portion>();

                if (portionAbove != null)
                {
                    Vector3 targetPos = new Vector3(x - spacingX, y - spacingY, portionAbove.transform.position.z);
                    portionAbove.MoveToTarget(targetPos);

                    portionAbove.SetIndicies(x, y);
                    portionBoard[x, y] = portionBoard[x, y + yOffset];
                    portionBoard[x, y + yOffset] = new Node(true, null);
                }
                else
                {
                    Debug.LogError($"portionAbove is null at {x}, {y + yOffset}");
                }
            }
        }
        else
        {
            SpawnPortionAtTop(x);
        }

        //we've either hit the top of the board or we found a portion
        // if (y + yOffset < height && portionBoard[x, y + yOffset].portion != null)
        // {
        //     //we've found a portion
        //     PortionBoard portionBoardInstance = FindObjectOfType<PortionBoard>(); // Get the PortionBoard instance
        //     Portion portionAbove = portionBoardInstance.portions[x, y + yOffset].GetComponent<Portion>();

        //     // Portion portionAbove = PortionBoard[x, y + yOffset].portion.GetComponent<Portion>();

        //     //Move it to the current location
        //     Vector3 targetPos = new Vector3(x - spacingX, y - spacingY, portionAbove.transform.position.z);
        //     portionAbove.MoveToTarget(targetPos);

        //     //Update indicies
        //     portionAbove.SetIndicies(x, y);
        //     portionBoard[x, y] = portionBoard[x, y + yOffset];

        //     //set the location the portion came from to null
        //     portionBoard[x, y + yOffset] = new Node(true, null);
        // } 

        // //if we've reached the top of the board without finding a portion
        // if (y + yOffset == height)
        // {
        //     SpawnPortionAtTop(x);
        // }
    }

    //SpawnPortionsAtTop()
    private void SpawnPortionAtTop(int x)
    {
        if (x < 0 || x >= boardWidth) {
        Debug.LogError($"Invalid index: {x}");
        return; // Prevent further execution
        }
        int index = FindIndexOfLowestNull(x);

        // Check if the index is valid (should be within the height of the board)
        if (index < 0 || index >= boardHeight) {
            Debug.LogError($"Invalid index found: {index} for column {x}");
            return; // Prevent further execution
        }

        // Calculate the location to move to based on index
        int locationToMoveTo = boardHeight - 1 - index; // Adjust based on your board height

        // Get a random portion
        if (portionPrefabs.Length == 0) {
            Debug.LogError("Portion prefabs array is empty!");
            return; // Prevent further execution
        }

        // int locationToMoveTo = 8 - index;
        //get a random portion
        int randomIndex = Random.Range(0, portionPrefabs.Length);

        GameObject newPortion = Instantiate(portionPrefabs[randomIndex], new Vector2(x - spacingX, height - spacingY), Quaternion.identity);
        newPortion.transform.SetParent(portionParent.transform);
        //set indicies
        newPortion.GetComponent<Portion>().SetIndicies(x, index);
        //set it on the board
        portionBoard[x, index] = new Node(true, newPortion);
        //move it to that location
        Vector3 targetPosition = new Vector3(newPortion.transform.position.x, newPortion.transform.position.y - locationToMoveTo, newPortion.transform.position.z);
        newPortion.GetComponent<Portion>().MoveToTarget(targetPosition);
    }

    //FindIndexOfLowestNull
    // private int FindIndexOfLowestNull(int x)
    // {
    //     int lowestNull = 99;
    //     for (int y = 7; y >= 0; y--)
    //     {
    //         if (portionBoard[x, y].portion == null)
    //         {
    //             lowestNull = y;
    //         }
    //     }
    //     return lowestNull;
    // }
    private int FindIndexOfLowestNull(int x)
    {
        // Check if x is within bounds of the portionBoard
        if (x < 0 || x >= boardWidth) {
            Debug.LogError($"Invalid column index: {x}");
            return -1; // Return -1 to indicate invalid column index
        }

        for (int y = boardHeight - 1; y >= 0; y--) // Start from the bottom of the board
        {
            if (portionBoard[x, y].portion == null)
            {
                return y; // Return the first found lowest null index
            }
        }

        // If no null was found, you can choose to return -1 or boardHeight
        return -1; // Indicate no available index
    }

    #endregion

    private MatchResult SuperMatch(MatchResult _matchedResult)
    {
        //if we have horizontal n long horizontal matches
        if (_matchedResult.direction == MatchDirection.Horizontal || _matchedResult.direction == MatchDirection.LongHorizontal)
        {
            //for each portion...
            foreach (Portion pot in _matchedResult.connectedPortions)
            {
                List<Portion> extraConnectedPortions = new();

                //checkDirection up
                CheckDirection(pot, new Vector2Int(0, 1), extraConnectedPortions);
                //checkDirection down
                CheckDirection(pot, new Vector2Int(0, -1), extraConnectedPortions);

                //do we have 2 or more extra matches?
                if (extraConnectedPortions.Count >= 2)
                {
                    Debug.Log("i have a super horizontal match");
                    extraConnectedPortions.AddRange(_matchedResult.connectedPortions);

                    //return super match
                    return new MatchResult
                    {
                        connectedPortions = extraConnectedPortions,
                        direction = MatchDirection.Super
                    };
                }
            }
            //we didnt have a super match, so return to normal
            return new MatchResult
            {
                connectedPortions = _matchedResult.connectedPortions,
                direction = _matchedResult.direction
            };
        }
        //if we have vertical n long vertical matches
        else if (_matchedResult.direction == MatchDirection.Vertical || _matchedResult.direction == MatchDirection.LongVertical)
        {
            //for each portion...
            foreach (Portion pot in _matchedResult.connectedPortions)
            {
                List<Portion> extraConnectedPortions = new();
                //checkDirection up
                CheckDirection(pot, new Vector2Int(1, 0), extraConnectedPortions);
                //checkDirection down
                CheckDirection(pot, new Vector2Int(-1, 0), extraConnectedPortions);

                //do we have 2 or more extra matches?
                if (extraConnectedPortions.Count >= 2)
                {
                    Debug.Log("i have a super vertical match");
                    extraConnectedPortions.AddRange(_matchedResult.connectedPortions);

                    //return super match
                    return new MatchResult
                    {
                        connectedPortions = extraConnectedPortions,
                        direction = MatchDirection.Super
                    };
                }
            }
            //we didnt have a super match, so return to normal
            return new MatchResult
            {
                connectedPortions = _matchedResult.connectedPortions,
                direction = _matchedResult.direction
            };
        }
        return null;
    }


    MatchResult IsConnected(Portion portion)
    {
        List<Portion> connectedPortions = new();
        PortionType portionType = portion.portionType;

        connectedPortions.Add(portion);

        // check right
        CheckDirection(portion, new Vector2Int(1, 0), connectedPortions);

        // check left
        CheckDirection(portion, new Vector2Int(-1, 0), connectedPortions);

        // have 3 match? (Horizontal)
        if (connectedPortions.Count == 3)
        {
            Debug.Log("Horizontal" + connectedPortions[0].portionType);
            return new MatchResult
            {
                connectedPortions = connectedPortions,
                direction = MatchDirection.Horizontal
            };
        }
        // if its more than 3 matches (long horizontal match)
        else if (connectedPortions.Count > 3)
        {
            Debug.Log("Horizontal" + connectedPortions[0].portionType);
            return new MatchResult
            {
                connectedPortions = connectedPortions,
                direction = MatchDirection.LongHorizontal
            };
        }
        //clear out the connectedPortions
        connectedPortions.Clear();

        //re-add our inititial portion
        connectedPortions.Add(portion);

        // check up
        CheckDirection(portion, new Vector2Int(0, 1), connectedPortions);

        // check down
        CheckDirection(portion, new Vector2Int(0, -1), connectedPortions);

        // have 3 match? (Vertical)
        if (connectedPortions.Count == 3)
        {
            Debug.Log("Vertical" + connectedPortions[0].portionType);
            return new MatchResult
            {
                connectedPortions = connectedPortions,
                direction = MatchDirection.Vertical
            };
        }
        // if its more than 3 matches (long vertical match)
        else if (connectedPortions.Count > 3)
        {
            Debug.Log("Vertical" + connectedPortions[0].portionType);
            return new MatchResult
            {
                connectedPortions = connectedPortions,
                direction = MatchDirection.LongVertical
            };
        }
        else
        {
            return new MatchResult
            {
                connectedPortions = connectedPortions,
                direction = MatchDirection.None
            };
        }
    }

    void CheckDirection(Portion pot, Vector2Int direction, List<Portion> connectedPortions)
    {
        PortionType portionType = pot.portionType;
        int x = pot.xIndex + direction.x;
        int y = pot.yIndex + direction.y;

        // check that we are within the bounderies of the board
        while (x >= 0 && x < width && y >= 0 && y < height)
        {
            if (portionBoard[x, y].isUsable)
            {
                Portion neighbourPortion = portionBoard[x, y].portion.GetComponent<Portion>();

                //does our portionType match? it must also not be matched
                if (!neighbourPortion.isMatched && neighbourPortion.portionType == portionType)
                {
                    connectedPortions.Add(neighbourPortion);

                    x += direction.x;
                    y += direction.y;
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
    }

    #region Swapping Portion
    //Select a portion
    public void SelectPortion(Portion _portion)
    {
        // if we dont have a portion selected, then set the portion i just clicked to my selectedportion
        if (selectedPortion == null)
        {
            Debug.Log(_portion);
            selectedPortion = _portion;
            // selectedPortion.isSelected = true;
        }
        //if we select the same portion twice, then lets make selectedportion null
        else if (selectedPortion == _portion)
        {
            selectedPortion = null;
        }
        //if selectedportion is not null and is not the current potion, attempt a swap
        //selectedportion back to null
        else if (selectedPortion != _portion)
        {
            SwapPortion(selectedPortion, _portion);
            selectedPortion = null;
        }
    }

    //Swap portion-logic
    private void SwapPortion(Portion _currentPortion, Portion _targetPortion)
    {
        //!isAdjacent dont do anything
        if (!IsAdjacent(_currentPortion, _targetPortion))
        {
            return;
        }

        //DOSWAP
        DoSwap(_currentPortion, _targetPortion);

        isProcessingMove = true;

        //startCoroutine ProcessMatches
        StartCoroutine(ProcessMatches(_currentPortion, _targetPortion));
    }

    //do swap
    private void DoSwap(Portion _currentPortion, Portion _targetPortion)
    {
        GameObject temp = portionBoard[_currentPortion.xIndex, _currentPortion.yIndex].portion;

        portionBoard[_currentPortion.xIndex, _currentPortion.yIndex].portion = portionBoard[_targetPortion.xIndex, _targetPortion.yIndex].portion;
        portionBoard[_targetPortion.xIndex, _targetPortion.yIndex].portion = temp;

        //update indices
        int tempXIndex = _currentPortion.xIndex;
        int tempYIndex = _currentPortion.yIndex;
        _currentPortion.xIndex = _targetPortion.xIndex;
        _currentPortion.yIndex = _targetPortion.yIndex;
        _targetPortion.xIndex = tempXIndex;
        _targetPortion.yIndex = tempYIndex;

        _currentPortion.MoveToTarget(portionBoard[_targetPortion.xIndex, _targetPortion.yIndex].portion.transform.position);
        _targetPortion.MoveToTarget(portionBoard[_currentPortion.xIndex, _currentPortion.yIndex].portion.transform.position);
    }

    private IEnumerator ProcessMatches(Portion _currentPortion, Portion _targetPortion)
    {
        yield return new WaitForSeconds(0.2f);
        bool hasMatched = CheckBoard(true);

        if (!hasMatched)
        {
            DoSwap(_currentPortion, _targetPortion);
        }
        isProcessingMove = false;
    }

    //IsAdjacent
    private bool IsAdjacent(Portion _currentPortion, Portion _targetPortion)
    {
        return Mathf.Abs(_currentPortion.xIndex - _targetPortion.xIndex) + Mathf.Abs(_currentPortion.yIndex - _targetPortion.yIndex) == 1;
    }

    //ProcessMatches

    #endregion

    public class MatchResult
    {
        public List<Portion> connectedPortions;
        public MatchDirection direction;
    }

    public enum MatchDirection
    {
        Horizontal,
        Vertical,
        LongVertical,
        LongHorizontal,
        Super,
        None
    }
}
