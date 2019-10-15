// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ServerSimulator.Properties
{
    class Settings {

        public static Settings Default {
            get {
                return new Settings();
            }
        }

        public int CacheSize {
            get {
                return 104857600;
            }
        }
        
        public float CacheReplacementRate {
            get {
                return 0.01f;
            }
        }
        
        public int NumRequests {
            get {
                return 200;
            }
        }
        
        public int AllocationVolume {
            get {
                return 100000;
            }
        }
        
        public float SurvivalRate {
            get {
                return 0.9f;
            }
        }
        
        public int StaticDataVolume {
            get {
                return 500;
            }
        }
        
        public int SteadyStateFactor {
            get {
                return 20;
            }
        }
        
        public int NumPasses {
            get {
                return 1;
            }
        }
        
        public bool Pinning {
            get {
                return false;
            }
        }
        
        public float FinalizableRate {
            get {
                return 0;
            }
        }
        
        public bool FifoCache {
            get {
                return false;
            }
        }
        
        public int RandomSeed {
            get {
                return 0;
            }
        }
    }
}