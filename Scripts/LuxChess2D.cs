using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text.RegularExpressions;

public class LuxChess2D : Engine {

    /// <summary>
    /// Pieces set.
    /// </summary>
    [Serializable]
    public class PieceSet
    {
        public Sprite Pawn;
        public Sprite Bishop;
        public Sprite Knight;
        public Sprite Rook;
        public Sprite Queen;
        public Sprite King;
        public Sprite Square;
    }

    /// <summary>
    /// White pieces.
    /// </summary>
    public PieceSet WhitePieces;

    /// <summary>
    /// Black pieces.
    /// </summary>
    public PieceSet BlackPieces;

    [HideInInspector]
    public Transform[] SquaresObj; //From 0-to 63
    public Transform CanvasTr; //Squares should be parented to the canvas
    public GameObject SquareObj; //Square prefab
    public Sprite NoPieceSprite; //If there is no piece then use this sprite for the piece image - should be transparent

    /// <summary>
    /// Comuputer thinking time. He will 'force' stop after this time.
    /// </summary>
    public int ComputerThinkingTime = 3;

    /// <summary>
    /// Starting FEn.
    /// </summary>
    public string StartingFen = FEN.Default;

    [HideInInspector]
    public int DragingFrom; //We are dragging piece from this position

    public UnityEvent OnWhiteTurn; //Event
    public UnityEvent OnBlackTurn; //Event

    //UI
    //public Text GameStateUI;
    public InputField FENOutputUI;

    private void Start() {

        DragingFrom = -1; //Not dragging from any square
        SquaresObj = new Transform[64]; //Inti squares

        for (int i = 0; i < 64; i++)
        {
            int x= i%8;
            int y = i/8;

            //Create square
            GameObject obj = Instantiate(SquareObj, Vector3.zero, Quaternion.identity) as GameObject;
            obj.transform.SetParent(CanvasTr, true); //set parent
            obj.transform.localPosition = new Vector3(-184 + 54 * x, 174 - 54 * y, 0); //Position it

            PieceMover pm = obj.GetComponent<PieceMover>(); //Get piece component
            pm.index = i; //Set index 
            pm.manager = this; //Set manager reference

            if ((i + y) % 2 == 0) //Change sprites according to squares
            {
                obj.GetComponent<Image>().sprite = WhitePieces.Square; //White square
            }
            else
                obj.GetComponent<Image>().sprite = BlackPieces.Square; //Black square

            //Save a square in a squares array at right index
            SquaresObj[i] = obj.transform;
        }

        //Calls the init in the engine class
        InitChess(StartingFen);
        
        //Updates the board changes
        UpdateBoard();

    }

    /// <summary>
    /// Updates all pieces based on board pieces.
    /// </summary>
    public void UpdateBoard() {
        //Initialize grid
        for (int i = 0; i < 64; i++)
        {
            int piece = GetPieceAt(i);

            switch (piece)
            {
                //Button will shrink texture, which creates much better 'piece' effect
                case Defs.WPawn: CreatePiece(WhitePieces.Pawn, i); break;
                case Defs.WBishop: CreatePiece(WhitePieces.Bishop, i); break;
                case Defs.WKnight: CreatePiece(WhitePieces.Knight, i); break;
                case Defs.WRook: CreatePiece(WhitePieces.Rook, i); break;
                case Defs.WQueen: CreatePiece(WhitePieces.Queen, i); break;
                case Defs.WKing: CreatePiece(WhitePieces.King, i); break;
                case Defs.BPawn: CreatePiece(BlackPieces.Pawn, i); break;
                case Defs.BBishop: CreatePiece(BlackPieces.Bishop, i); break;
                case Defs.BKnight: CreatePiece(BlackPieces.Knight, i); break;
                case Defs.BRook: CreatePiece(BlackPieces.Rook, i); break;
                case Defs.BQueen: CreatePiece(BlackPieces.Queen, i); break;
                case Defs.BKing: CreatePiece(BlackPieces.King, i); break;
                case Defs.Empty:
                    CreatePiece(NoPieceSprite, i);
                    break;
            }

        }

    
    }


    private Squares LastSquare = Squares.None;
    public override void OnTurnSwitched() //Turn was switched
    { 

        //Who's turn?
        if (SideToPlay() == 1) //White
        {
            if (OnWhiteTurn != null)
                OnWhiteTurn.Invoke();
        }
        else {
            if (OnBlackTurn != null)
                OnBlackTurn.Invoke();
            ComputerPlay(ComputerThinkingTime); //Computer should play as black
        }

        //GameStateUI.text = Regex.Replace(GameState().ToString(),"[A-Z]", " $0"); //Space before capital letter
        //FENOutputUI.text = GetFen();

        if (GameState() == BoardState.WhiteMate)
        {
            GameManager.isGameOver = true;
            GameManager.gameOverState = "WhiteMate";
        }
        if (GameState() == BoardState.BlackMate)
        {
            GameManager.isGameOver = true;
            GameManager.gameOverState = "BlackMate";
        }
        if (GameState() == BoardState.StaleMate)
        {
            GameManager.isGameOver = true;
            GameManager.gameOverState = "StaleMate";
        }
        

        //Check if in check
        Squares sq = IsInCheck();
        if (sq != Squares.None) { //If in check mark square
            LastSquare = sq;
            SquaresObj[(int)LastSquare].gameObject.GetComponent<Image>().color = new Color32(255,15,15,255);
            GameManager.Instance.PlayChessDownAudio(GameManager.Instance.clips[3]);

        }
        else if (LastSquare != Squares.None) {//Remove last marked square
            SquaresObj[(int)LastSquare].gameObject.GetComponent<Image>().color = Color.white;
        }
    }



    private Vector2 origPieceSize = Vector2.zero;
    private void CreatePiece(Sprite piece, int index, float scale = 1f)
    {
        Image image = SquaresObj[index].GetChild(0).gameObject.GetComponent<Image>();

        //Get the size of the first piece and store it 
        if (origPieceSize == Vector2.zero)
            origPieceSize = image.rectTransform.sizeDelta;

        image.rectTransform.sizeDelta = origPieceSize*scale;
        image.sprite = piece;

    }

    private Squares LastSquareFrom = Squares.None;
    private Squares LastSquareTo = Squares.None;
    public override void OnComputerPlayed(int from, int to)
    {

        //Mark the squares that piece traveled
        SquaresObj[(int)from].gameObject.GetComponent<Image>().color = new Color32(49, 73, 54, 255);
        SquaresObj[(int)to].gameObject.GetComponent<Image>().color = new Color32(36, 62, 46, 255);

        //Reset colors
        if ((int)LastSquareFrom != from && (int)LastSquareFrom != to && LastSquareFrom != Squares.None)
        {
            SquaresObj[(int)LastSquareFrom].gameObject.GetComponent<Image>().color = Color.white;
        }

        if ((int)LastSquareTo != to && (int)LastSquareTo != from && LastSquareTo != Squares.None)
        {
            SquaresObj[(int)LastSquareTo].gameObject.GetComponent<Image>().color = Color.white;
        }

        //Store marked squares
        LastSquareFrom = (Squares)from;
        LastSquareTo = (Squares)to;

        //Since board was changed by computer update board
        UpdateBoard();
    }


    public void Undo() {

        GameManager.Instance.PlayChessDownAudio(GameManager.Instance.clips[1]);


        //Calls the engine undo move
        UndoMove();

        //Reset color
        SquaresObj[(int)LastSquareFrom].gameObject.GetComponent<Image>().color = Color.white;
        SquaresObj[(int)LastSquareTo].gameObject.GetComponent<Image>().color = Color.white;
        
        //Sinde the board has changed update the move
        UpdateBoard();
    }

}
