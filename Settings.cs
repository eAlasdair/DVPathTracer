using UnityModManagerNet;

namespace DVPathTracer
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public string version;

        [Draw("Report rate (seconds per report)", Min = 1f)]
        public float logRate = 5f;

        [Draw("Record speed in mph (default: km/h)")]
        public bool mph = false;

        [Draw("Active")]
        public bool isActive = true;

        [Draw("Prevent activation on startup")]
        public bool forceStartInactive = false;

        [Draw("Save each session to a new file")]
        public bool useSystemTime = true;

        override public void Save(UnityModManager.ModEntry entry)
        {
            Save(this, entry);
        }

        public void OnChange()
        {
        }
    }
}
