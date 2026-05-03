using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;
                    
    void Start()
    {        
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }
        
    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;            

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;                
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();                         
            }
        }
                
        cops[0].GetComponent<CopMove>().currentTile=Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile=Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile=Constants.InitialRobber;           
    }

    public void InitAdjacencyLists()
    {
        // Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        // Inicializar matriz a 0
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                matriu[i, j] = 0;
            }
        }

        // Para cada casilla, marcamos sus vecinos (arriba, abajo, izquierda, derecha)
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            int fila = i / Constants.TilesPerRow;
            int columna = i % Constants.TilesPerRow;

            // Abajo
            if (fila > 0)
                matriu[i, i - Constants.TilesPerRow] = 1;

            // Arriba
            if (fila < Constants.TilesPerRow - 1)
                matriu[i, i + Constants.TilesPerRow] = 1;

            // Izquierda
            if (columna > 0)
                matriu[i, i - 1] = 1;

            // Derecha
            if (columna < Constants.TilesPerRow - 1)
                matriu[i, i + 1] = 1;
        }

        // Rellenar adjacency list de cada Tile
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            tiles[i].adjacency.Clear();

            for (int j = 0; j < Constants.NumTiles; j++)
            {
                if (matriu[i, j] == 1)
                {
                    tiles[i].adjacency.Add(j);
                }
            }
        }
    }

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {        
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:                
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;                
                break;            
        }
    }

    public void ClickOnTile(int t)
    {                     
        clickedTile = t;

        switch (state)
        {            
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile = tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;

                    state = Constants.TileSelected;
                }
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {            
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:                
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        ResetTiles();

        // Posición actual del ladrón
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;

        // Calculamos las casillas alcanzables por el ladrón
        FindSelectableTiles(false);

        // Lista de casillas seleccionables
        List<Tile> selectableTiles = new List<Tile>();

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (tiles[i].selectable)
            {
                selectableTiles.Add(tiles[i]);
            }
        }

        if (selectableTiles.Count > 0)
        {
            Tile bestTile = selectableTiles[0];
            int bestDistance = -1;

            for (int i = 0; i < selectableTiles.Count; i++)
            {
                Tile candidate = selectableTiles[i];

                int distanceToCop0 = GetDistance(candidate.numTile, cops[0].GetComponent<CopMove>().currentTile);
                int distanceToCop1 = GetDistance(candidate.numTile, cops[1].GetComponent<CopMove>().currentTile);

                // Nos quedamos con la distancia al policía más cercano
                int minDistanceToCops = Mathf.Min(distanceToCop0, distanceToCop1);

                // Elegimos la casilla cuya distancia mínima sea mayor
                if (minDistanceToCops > bestDistance)
                {
                    bestDistance = minDistanceToCops;
                    bestTile = candidate;
                }
            }

            // Movemos el ladrón a la casilla más segura
            robber.GetComponent<RobberMove>().MoveToTile(bestTile);

            // Actualizamos su posición
            robber.GetComponent<RobberMove>().currentTile = bestTile.numTile;
        }
    }

    public void EndGame(bool end)
    {
        if (end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }
    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);
                
        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;
         
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
        int indexcurrentTile;

        if (cop == true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        // La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        // Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        // Marcamos la casilla inicial como visitada
        tiles[indexcurrentTile].visited = true;
        tiles[indexcurrentTile].distance = 0;

        // Metemos la casilla inicial en la cola
        nodes.Enqueue(tiles[indexcurrentTile]);

        // BFS limitado a distancia 2
        while (nodes.Count > 0)
        {
            Tile actual = nodes.Dequeue();

            // Si ya hemos llegado a la distancia máxima, no seguimos expandiendo
            if (actual.distance == Constants.Distance)
            {
                continue;
            }

            // Recorremos todos los vecinos de la casilla actual
            foreach (int vecino in actual.adjacency)
            {
                Tile tileVecino = tiles[vecino];

                // Si mueve un policía, no puede pasar por la casilla del otro policía
                if (cop == true)
                {
                    int otroPolicia;

                    if (clickedCop == 0)
                        otroPolicia = cops[1].GetComponent<CopMove>().currentTile;
                    else
                        otroPolicia = cops[0].GetComponent<CopMove>().currentTile;

                    if (vecino == otroPolicia)
                    {
                        continue;
                    }
                }

                // Si el vecino no ha sido visitado, lo añadimos al BFS
                if (!tileVecino.visited)
                {
                    tileVecino.visited = true;
                    tileVecino.parent = actual;
                    tileVecino.distance = actual.distance + 1;

                    // La ficha no puede quedarse en su casilla actual
                    if (tileVecino.numTile != indexcurrentTile)
                    {
                        tileVecino.selectable = true;
                    }

                    nodes.Enqueue(tileVecino);
                }
            }
        }


    }




    public int GetDistance(int startTile, int targetTile)
    {
        bool[] visited = new bool[Constants.NumTiles];
        int[] distance = new int[Constants.NumTiles];

        Queue<int> queue = new Queue<int>();

        visited[startTile] = true;
        distance[startTile] = 0;
        queue.Enqueue(startTile);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (current == targetTile)
            {
                return distance[current];
            }

            foreach (int vecino in tiles[current].adjacency)
            {
                if (!visited[vecino])
                {
                    visited[vecino] = true;
                    distance[vecino] = distance[current] + 1;
                    queue.Enqueue(vecino);
                }
            }
        }

        return 999;
    }




}
