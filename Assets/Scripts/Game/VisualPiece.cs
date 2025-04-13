using System.Collections.Generic;
using Unity.Netcode;
using UnityChess;
using UnityEngine;
using static UnityChess.SquareUtil;

/// <summary>
/// Represents a visual chess piece in the game. This component handles user interaction,
/// such as dragging and dropping pieces, and determines the closest square on the board
/// where the piece should land. It also raises an event when a piece has been moved.
/// </summary>
public class VisualPiece : NetworkBehaviour
{
    // Delegate for handling the event when a visual piece has been moved.
    // Parameters: the initial square of the piece, its transform, the closest square's transform,
    // and an optional promotion piece.
    public delegate void VisualPieceMovedAction(Square movedPieceInitialSquare, Transform movedPieceTransform, Transform closestBoardSquareTransform, Piece promotionPiece = null);

    // Static event raised when a visual piece is moved.
    public static event VisualPieceMovedAction VisualPieceMoved;

    // The colour (side) of the piece (White or Black).
    public Side PieceColor;

    // Retrieves the current board square of the piece by converting its parent's name into a Square.
    public Square CurrentSquare => StringToSquare(transform.parent.name);

    // The radius used to detect nearby board squares for collision detection.
    private const float SquareCollisionRadius = 9f;

    // The camera used to view the board.
    private Camera boardCamera;
    // The screen-space position of the piece when it is first picked up.
    private Vector3 piecePositionSS;
    // A reference to the piece's SphereCollider (if required for collision handling).
    private SphereCollider pieceBoundingSphere;
    // A list to hold potential board square GameObjects that the piece might land on.
    private List<GameObject> potentialLandingSquares;
    // A cached reference to the transform of this piece.
    private Transform thisTransform;

    /// <summary>
    /// Initialises the visual piece. Sets up necessary variables and obtains a reference to the main camera.
    /// </summary>
    private void Start()
    {
        // Initialise the list to hold potential landing squares.
        potentialLandingSquares = new List<GameObject>();
        // Cache the transform of this GameObject for efficiency.
        thisTransform = transform;
        // Obtain the main camera from the scene.
        boardCamera = Camera.main;

        //// Disable interaction if this player is not allowed to move this piece
        //if (!CanPlayerMovePiece())
        //{
        //    enabled = false; // Disable this script
        //}
    }

    /// <summary>
    /// Determines whether the current player can move this piece.
    /// </summary>
    /// <returns>True if the player can move this piece; otherwise, false.</returns>
    private bool CanPlayerMovePiece()
    {
        Side currentTurn = GameManager.Instance.GetCurrentTurn();

        // Host can only move white pieces; client can only move black pieces
        if (IsHost)
        {
            return PieceColor == Side.White && currentTurn == Side.White;
        }
        if (!IsHost && IsClient)
        {
            return PieceColor == Side.Black && currentTurn == Side.Black;
        }
        return false;
    }

    /// <summary>
    /// Called when the user presses the mouse button over the piece.
    /// Records the initial screen-space position of the piece.
    /// </summary>
    public void OnMouseDown()
    {
        if (enabled && CanPlayerMovePiece())
        {
            // Convert the world position of the piece to screen-space and store it.
            Vector3 position = boardCamera.WorldToScreenPoint(transform.position);
            piecePositionSS.z = position.z;
            OnMouseDownServerRpc(position.x, position.y);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnMouseDownServerRpc(float posX, float posY)
    {
        piecePositionSS.x = posX;
        piecePositionSS.y = posY;
    }

    /// <summary>
    /// Called while the user drags the piece with the mouse.
    /// Updates the piece's world position to follow the mouse cursor.
    /// </summary>
    private void OnMouseDrag()
    {
        if (enabled && CanPlayerMovePiece())
        {
            // Create a new screen-space position based on the current mouse position,
            // preserving the original depth (z-coordinate).
            Vector3 nextPiecePositionSS = new Vector3(Input.mousePosition.x, Input.mousePosition.y, piecePositionSS.z);
            // Convert the screen-space position back to world-space and update the piece's position.
            Vector3 position = boardCamera.ScreenToWorldPoint(nextPiecePositionSS);
            OnMouseDragServerRpc(position);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnMouseDragServerRpc(Vector3 position)
    {
        thisTransform.position = position;
    }

    /// <summary>
    /// Called when the user releases the mouse button after dragging the piece.
    /// Determines the closest board square to the piece and raises an event with the move.
    /// </summary>
    public void OnMouseUp()
    {
        if (enabled && CanPlayerMovePiece())
        {
            MovePieceServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void MovePieceServerRpc()
    {
        // Clear any previous potential landing square candidates.
        potentialLandingSquares.Clear();
        // Obtain all square GameObjects within the collision radius of the piece's current position.
        BoardManager.Instance.GetSquareGOsWithinRadius(potentialLandingSquares, thisTransform.position, SquareCollisionRadius);

        // If no squares are found, assume the piece was moved off the board and reset its position.
        if (potentialLandingSquares.Count == 0)
        { // piece moved off board
            thisTransform.position = thisTransform.parent.position;
            return;
        }

        // Determine the closest square from the list of potential landing squares.
        Transform closestSquareTransform = potentialLandingSquares[0].transform;
        // Calculate the square of the distance between the piece and the first candidate square.
        float shortestDistanceFromPieceSquared = (closestSquareTransform.position - thisTransform.position).sqrMagnitude;

        // Iterate through remaining potential squares to find the closest one.
        for (int i = 1; i < potentialLandingSquares.Count; i++)
        {
            GameObject potentialLandingSquare = potentialLandingSquares[i];
            // Calculate the squared distance from the piece to the candidate square.
            float distanceFromPieceSquared = (potentialLandingSquare.transform.position - thisTransform.position).sqrMagnitude;

            // If the current candidate is closer than the previous closest, update the closest square.
            if (distanceFromPieceSquared < shortestDistanceFromPieceSquared)
            {
                shortestDistanceFromPieceSquared = distanceFromPieceSquared;
                closestSquareTransform = potentialLandingSquare.transform;
            }
        }

        // Raise the VisualPieceMoved event with the initial square, the piece's transform, and the closest square transform.
        VisualPieceMoved?.Invoke(CurrentSquare, thisTransform, closestSquareTransform);
    }

    public void UpdateInteractability()
    {
        enabled = CanPlayerMovePiece();
    }
}
