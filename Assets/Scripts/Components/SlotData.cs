﻿using System;
using System.ComponentModel;
using Unity.Entities;

[Serializable]
public struct SlotReference : IComponentData
{
    public Entity Value;
}