using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; // Allows us to hook up actions easily in the Inspector

public class PuzzleBoard : MonoBehaviour
{
    [Header("Tag Settings")]
    [Tooltip("The tag assigned to your big wooden blocks.")]
    [SerializeField] private string bigBlockTag = "BigBlock";
    
    [Tooltip("The tag assigned to your small wooden blocks.")]
    [SerializeField] private string smallBlockTag = "SmallBlock";

    [Header("Puzzle Requirements")]
    [SerializeField] private int requiredBigBlocks = 1;
    [SerializeField] private int requiredSmallBlocks = 2;

    [Header("Events")]
    [Tooltip("Triggered once the correct combination of blocks is placed on the board.")]
    public UnityEvent onPuzzleSolved;
    
    [Tooltip("Triggered if a block is removed and the puzzle becomes unsolved.")]
    [SerializeField] private UnityEvent onPuzzleUnsolved;

    // A collection to keep track of valid blocks currently resting on the board
    private HashSet<GameObject> blocksOnBoard = new HashSet<GameObject>();
    private bool isSolved = false;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object matches either of our block tags
        if (other.CompareTag(bigBlockTag) || other.CompareTag(smallBlockTag))
        {
            // Add the object to our tracking set (HashSet prevents duplicate entries)
            if (blocksOnBoard.Add(other.gameObject))
            {
                EvaluatePuzzle();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // If a tracked block leaves the board trigger area, remove it
        if (blocksOnBoard.Contains(other.gameObject))
        {
            blocksOnBoard.Remove(other.gameObject);
            EvaluatePuzzle();
        }
    }

    private void EvaluatePuzzle()
    {
        int currentBigCount = 0;
        int currentSmallCount = 0;

        // Count how many big and small blocks are currently on the board
        foreach (GameObject block in blocksOnBoard)
        {
            if (block != null)
            {
                if (block.CompareTag(bigBlockTag))
                {
                    currentBigCount++;
                }
                else if (block.CompareTag(smallBlockTag))
                {
                    currentSmallCount++;
                }
            }
        }

        // Check if the current block counts match the required amounts
        bool conditionsMet = (currentBigCount == requiredBigBlocks && currentSmallCount == requiredSmallBlocks);

        // If the puzzle is newly solved
        if (conditionsMet && !isSolved)
        {
            isSolved = true;
            Debug.Log("Puzzle Solved! Unlocking reward...");
            onPuzzleSolved?.Invoke(); // Fires any functions assigned to this event in the Inspector
        }
        // If it was solved, but a block was removed
        else if (!conditionsMet && isSolved)
        {
            isSolved = false;
            Debug.Log("Puzzle Unsolved - a block was removed.");
            onPuzzleUnsolved?.Invoke();
        }
    }
}