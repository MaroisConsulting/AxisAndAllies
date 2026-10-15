using Models;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public PieceModelBase PieceModel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //TODO: Load from JSON
        PieceModel = new InfantryModel
        {
            Cost = 3,
            Move = 1,
            Attack = 1,
            Defense = 2,
            HitsRequiredToDestroy = 1
        };
    }

    // Update is called once per frame
    void Update()
    {
    }
}
