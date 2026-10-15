using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Models
{
    public class GameSettings
    {
        public float MasterVolume { get; set; } = 1f;
        public bool Fullscreen { get; set; } = true;
        public int ResolutionWidth { get; set; } = 1920;
        public int ResolutionHeight { get; set; } = 1080;
        public string LastSaveFile { get; set; }
    }
}
