﻿using System;
using Unity.Entities;

namespace UndergroundMatch3.Components
{
    [Serializable]
    public struct Score : IComponentData
    {
        public int Value;
    }
}