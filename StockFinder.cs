using System.Collections.Generic;

namespace DVPathTracer
{
    public static class StockFinder
    {
        // <Order ID, car> To preserve order and location within the reports
        private static Dictionary<int, StockReporter> trackedStock = new Dictionary<int, StockReporter>();
        public static Dictionary<int, StockReporter> TrackedStock
        {
            get
            {
                return trackedStock;
            }
        }

        public static int numStock = 0;

        /**
         * Inserts the given TrainCar into the earliest available space
         */
        public static void Add(TrainCar car)
        {
            int i = 0;
            while (trackedStock.ContainsKey(i) && trackedStock[i] != null)
            {
                i++;
            }
            trackedStock[i] = new StockReporter(car);
            numStock++;
        }

        /**
         * Returns true if the car is being tracked, false otherwise
         */
        public static bool IsTracking(TrainCar car)
        {
            foreach (int index in trackedStock.Keys)
            {
                if (trackedStock[index] != null && trackedStock[index].Target == car)
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * Removes the given car, freeing that space for a new one
         */
        public static void Remove(StockReporter car)
        {
            foreach (int index in trackedStock.Keys)
            {
                if (trackedStock[index] == car)
                {
                    Remove(index);
                    return;
                }
            }
        }
        public static void Remove(TrainCar car)
        {
            foreach (int index in trackedStock.Keys)
            {
                if (trackedStock[index].Target == car)
                {
                    Remove(index);
                    return;
                }
            }
        }
        public static void Remove(int carIndex)
        {
            trackedStock[carIndex] = null;
            numStock--;
        }

        /**
         * Updates its dictionary of all new avaliable rolling stock to be tracked
         * TODO: Optimise somehow because this is definitely not an efficient task
         */
        public static void UpdateTrackedStock()
        {
            TrainCar[] allCars = UnityEngine.Object.FindObjectsOfType<TrainCar>();
            foreach (TrainCar car in allCars)
            {
                //if (car.IsLoco || car.carType == TrainCarType.CabooseRed)
                //{
                    if (!IsTracking(car))
                    {
                        Add(car);
                    }
                //}
            }
        }
    }
}
