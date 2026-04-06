using System.Collections.Generic;
using UnityEngine;

public struct MoveRecord
{
    public Vector3 playerPos;
    public Furniture pushedFurniture;
    public Vector3 furniturePos;
    public bool wasPush;
}

public static class UndoManager
{
    private static Stack<MoveRecord> history = new Stack<MoveRecord>();

    public static void RecordMove(Vector3 pPos, Furniture f = null, Vector3 fPos = default)
    {
        history.Push(new MoveRecord{playerPos = pPos,pushedFurniture = f,furniturePos = fPos,wasPush = f != null});
    }

    public static void UndoMove(Player player)
    {
        //Si no hay historial, no hacemos nada
        if (history.Count == 0 || player == null) return;

        MoveRecord lastMove = history.Pop();

        //1. Detenemos las corrutinas por si el jugador
        player.StopAllCoroutines();

        //2. Devolvemos al jugador
        player.transform.position = lastMove.playerPos;
        Stats.movesCount--;

        //3. Devolvemos el mueble
        if (lastMove.wasPush && lastMove.pushedFurniture != null)
        {
            lastMove.pushedFurniture.StopAllCoroutines();
            lastMove.pushedFurniture.transform.position = lastMove.furniturePos;
            Stats.pushCount--;
        }

        //Reactivamos el movimiento del jugador por si se quedó pillado al interrumpir la corrutina
        player.ResetMovementState();
    }

    public static void ClearHistory()
    {
        history.Clear();
    }
}
