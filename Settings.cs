using UnityModManagerNet;

namespace DVPathTracer
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public string version;
        public string fileName = "DVTracedPath.csv";
        public bool forceStartInactive = true;

        [Draw("Active")]
        public bool isActive = false;

        [Draw("Report rate (seconds per report)", Min = 1f)]
        public float logRate = 5f;
        
        override public void Save(UnityModManager.ModEntry entry)
        {
            Save(this, entry);
        }

        public void OnChange()
        {
        }
    }
}
