using DVCustomCarLoader;

namespace DVPathTracer
{
    // Separate file only used if CCL is installed to avoid System.IO.FileNotFoundException
    internal static class CCLInterface
    {
        /**
         * Return the human-friendly name of a CCL car
         */
        internal static string CustomCarIndentifier(TrainCarType car)
        {
            return CarTypeInjector.CustomCarByType(car).identifier;
        }
    }
}
