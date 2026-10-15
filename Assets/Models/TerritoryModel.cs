using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class TerritoryModel
    {
        // A default constructor lets JSON restore shared/cyclic neighbor references.
        public TerritoryModel() : this("Unnamed", 0, false, Countries.US, null, null) { }

        public string Name;
        public int IPC;
        public bool IsNeutral = false;
        public Countries player;
        public List<TerritoryModel> Neighbors;
        public List<PieceModelBase> Pieces;

        public TerritoryModel(
            string name,
            int ipc,
            bool isNeutral,
            Countries player,
            List<TerritoryModel> neighbors,
            List<PieceModelBase> pieces)
        {
            Name = name;
            IPC = ipc;
            IsNeutral = isNeutral;
            this.player = player;
            Neighbors = neighbors ?? new List<TerritoryModel>();
            Pieces = pieces ?? new List<PieceModelBase>();
        }

        public string Info()
        {
            var info = new StringBuilder();
            info.AppendLine($"Territory: {Name ?? "Unnamed"}");
            info.AppendLine($"IPC: {IPC}");
            info.AppendLine($"Neutral: {(IsNeutral ? "Yes" : "No")}");
            info.AppendLine($"Player: {player}");

            info.AppendLine($"Neighbors ({Neighbors?.Count ?? 0}):");
            if (Neighbors == null || Neighbors.Count == 0)
            {
                info.AppendLine("  None");
            }
            else
            {
                foreach (var neighbor in Neighbors)
                {
                    info.AppendLine($"  - {neighbor?.Name ?? "Unnamed"}");
                }
            }

            info.AppendLine($"Pieces ({Pieces?.Count ?? 0}):");
            if (Pieces == null || Pieces.Count == 0)
            {
                info.AppendLine("  None");
            }
            else
            {
                foreach (var piece in Pieces)
                {
                    if (piece == null)
                    {
                        info.AppendLine("  - Missing piece");
                        continue;
                    }

                    info.AppendLine($"  - {piece.GetType().Name} (Owner: {piece.Owner})");
                    info.AppendLine($"    Cost: {piece.Cost}, Move: {piece.Move}, Attack: {piece.Attack}, Defense: {piece.Defense}, Hits to destroy: {piece.HitsRequiredToDestroy}");
                }
            }

            return info.ToString().TrimEnd();
        }
    }
}
