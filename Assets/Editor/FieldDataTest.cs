﻿using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using NUnit.Framework;
using UndergroundMatch3;
using UndergroundMatch3.Components;
using UndergroundMatch3.Data;
using UndergroundMatch3.Data.Steps;
using UndergroundMatch3.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[TestFixture]
public class FieldDataTest : BaseWorldTests
{
    [Test]
    public void PositionToIdTest()
    {
        var levelDescription = new LevelDescription();
        levelDescription.Width = 2;
        levelDescription.Height = 2;
        levelDescription.ColorCount = 5;
        levelDescription.Time = 60;

        var game = new GameObject().AddComponent<Game>();
        game.SceneConfiguration = CreateTestSceneConfiguration();
        var sceneConfiguration = game.SceneConfiguration;
        sceneConfiguration.Level = ScriptableObject.CreateInstance<LevelDescriptionAsset>();
        sceneConfiguration.Level.Value = levelDescription;

        sceneConfiguration.Center = new GameObject("Center").transform;
        sceneConfiguration.Center.position = new Vector3(1,1,0);

        Assert.AreEqual(new int2(0,0), sceneConfiguration.GetIndex(new Vector3(0.5f, 0.5f)));
        Assert.AreEqual(new int2(1, 0), sceneConfiguration.GetIndex(new Vector3(1.5f, 0.5f)));
        Assert.AreEqual(new int2(0, 1), sceneConfiguration.GetIndex(new Vector3(0.5f, 1.5f)));
        Assert.AreEqual(new int2(1, 1), sceneConfiguration.GetIndex(new Vector3(1.5f, 1.5f)));
    }



    [Test]
    public void CreateSlots()
    {
        var entityManager = World.Active.GetOrCreateManager<EntityManager>();

        var levelDescription = new LevelDescription()
        {
            Width = 5,
            Height = 5
        };

        var sceneConfiguration = CreateTestSceneConfiguration();

        var creationPipeline = new CreateSlotsStep(new Dictionary<int2, Entity>(), sceneConfiguration);
        creationPipeline.Apply(levelDescription, entityManager);

        //check
        var entities = entityManager.GetAllEntities();
        var slotEntityCount = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            if (entityManager.HasComponent<Slot>(entities[i]))
            {
                slotEntityCount++;
            }
        }
        entities.Dispose();

        Assert.AreEqual(25, slotEntityCount);
    }

    [Test]
    public void CreateChips()
    {
        var entityManager = World.Active.GetOrCreateManager<EntityManager>();

        var levelDescription = new LevelDescription()
        {
            Width = 5,
            Height = 5
        };

        var sceneConfiguration = CreateTestSceneConfiguration();
        var configuration = CreateTestConfiguration();

        var createSlots = new CreateSlotsStep(new Dictionary<int2, Entity>(), sceneConfiguration);
        createSlots.Apply(levelDescription, entityManager);

        var createChips = new CreateChipsStep(configuration);
        createChips.Apply(levelDescription, entityManager);

        //check
        var entities = entityManager.GetAllEntities();
        var countChips = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            if (entityManager.HasComponent<Chip>(entities[i]))
            {
                countChips++;
            }
        }
        entities.Dispose();

        Assert.AreEqual(25, countChips);
    }

    [Test]
    public void CreateChipsAndFindSimpleLineCombinations()
    {
        var entityManager = World.Active.GetOrCreateManager<EntityManager>();

        var levelDescription = new LevelDescription()
        {
            Width = 3,
            Height = 3,
            SlotChipDescriptions = new List<SlotChipDescription>()
            {
                new SlotChipDescription()
                {
                    Position = new int2(0,0),
                    Color = ChipColor.Red,
                },
                new SlotChipDescription()
                {
                    Position = new int2(1,0),
                    Color = ChipColor.Red,
                },
                new SlotChipDescription()
                {
                    Position = new int2(2,0),
                    Color = ChipColor.Red,
                },

                new SlotChipDescription()
                {
                    Position = new int2(0,1),
                    Color = ChipColor.Blue,
                },
                new SlotChipDescription()
                {
                    Position = new int2(1,1),
                    Color = ChipColor.Blue,
                },
                new SlotChipDescription()
                {
                    Position = new int2(2,1),
                    Color = ChipColor.Blue,
                },

                new SlotChipDescription()
                {
                    Position = new int2(0,2),
                    Color = ChipColor.Yellow,
                },
                new SlotChipDescription()
                {
                    Position = new int2(1,2),
                    Color = ChipColor.Yellow,
                },
                new SlotChipDescription()
                {
                    Position = new int2(2,2),
                    Color = ChipColor.Yellow,
                },

            }
        };

        var slotCache = new Dictionary<int2, Entity>();
        var sceneConfiguration = CreateTestSceneConfiguration();
        var configuration = CreateTestConfiguration();
        var createSlots = new CreateSlotsStep(slotCache, sceneConfiguration);
        createSlots.Apply(levelDescription, entityManager);


        var createChips = new CreateChipsStep(configuration);
        createChips.Apply(levelDescription, entityManager);

        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        var combinationList = new NativeList<Entity>(64, Allocator.Temp);
        var visited = new NativeHashMap<int2, bool>(64, Allocator.Temp);

        FindCombinationsSystem.Find(entityManager, slotCache, new int2(0,0), ref visited, ref combinationList);
        Assert.AreEqual(3, combinationList.Length);

        combinationList.Clear();
        FindCombinationsSystem.Find(entityManager, slotCache,  new int2(0,1), ref visited, ref combinationList);
        Assert.AreEqual(3, combinationList.Length);

        combinationList.Clear();
        FindCombinationsSystem.Find(entityManager,slotCache,  new int2(0,2), ref visited, ref combinationList);
        Assert.AreEqual(3, combinationList.Length);

        Assert.IsTrue(FindCombinationsSystem.IsCorrectCombination(entityManager, combinationList, levelDescription.Width, levelDescription.Height));


        combinationList.Dispose();
    }

    [Test]
    public void CreateChipsAndFindOneBigCombination()
    {
        var entityManager = World.Active.GetOrCreateManager<EntityManager>();

        var levelDescription = new LevelDescription()
        {
            Width = 3,
            Height = 3,
            SlotChipDescriptions = new List<SlotChipDescription>()
            {
                new SlotChipDescription()
                {
                    Position = new int2(0,0),
                    Color = ChipColor.Red,
                },
                new SlotChipDescription()
                {
                    Position = new int2(1,0),
                    Color = ChipColor.Red,
                },
                new SlotChipDescription()
                {
                    Position = new int2(2,0),
                    Color = ChipColor.Red,
                },

                new SlotChipDescription()
                {
                    Position = new int2(0,1),
                    Color = ChipColor.Red,
                },
                new SlotChipDescription()
                {
                    Position = new int2(1,1),
                    Color = ChipColor.Red,
                },
                new SlotChipDescription()
                {
                    Position = new int2(2,1),
                    Color = ChipColor.Red,
                },

                new SlotChipDescription()
                {
                    Position = new int2(0,2),
                    Color = ChipColor.Red,
                },
                new SlotChipDescription()
                {
                    Position = new int2(1,2),
                    Color = ChipColor.Red,
                },
                new SlotChipDescription()
                {
                    Position = new int2(2,2),
                    Color = ChipColor.Red,
                },

            }
        };

        var sceneConfiguration = CreateTestSceneConfiguration();
        var configuration = CreateTestConfiguration();

        var slotCache = new Dictionary<int2, Entity>();
        var createSlots = new CreateSlotsStep(slotCache, sceneConfiguration);
        createSlots.Apply(levelDescription, entityManager);


        var createChips = new CreateChipsStep(configuration);
        createChips.Apply(levelDescription, entityManager);

        var combinationList = new NativeList<Entity>(64, Allocator.Temp);

        var visited = new NativeHashMap<int2, bool>(64, Allocator.Temp);

        FindCombinationsSystem.Find(entityManager, slotCache,  new int2(0,0), ref visited, ref combinationList);
        Assert.AreEqual(9, combinationList.Length);

        visited.Clear();
        combinationList.Clear();
        FindCombinationsSystem.Find(entityManager,slotCache,  new int2(0,1), ref visited, ref combinationList);
        Assert.AreEqual(9, combinationList.Length);

        visited.Clear();
        combinationList.Clear();
        FindCombinationsSystem.Find(entityManager, slotCache,new int2(0,2),ref visited,  ref combinationList);
        Assert.AreEqual(9, combinationList.Length);

        visited.Dispose();
        combinationList.Dispose();
    }

    [Test]
    public void CreateChipsAndFindThreeGroupButNoCombinations()
    {
        var entityManager = World.Active.GetOrCreateManager<EntityManager>();

        var levelDescription = new LevelDescription()
        {
            Width = 3,
            Height = 3,
            SlotChipDescriptions = new List<SlotChipDescription>()
            {
                new SlotChipDescription()
                {
                    Position = new int2(0,0),
                    Color = ChipColor.Red,
                },
                new SlotChipDescription()
                {
                    Position = new int2(1,0),
                    Color = ChipColor.Red,
                },
                new SlotChipDescription()
                {
                    Position = new int2(2,0),
                    Color = ChipColor.Blue,
                },

                new SlotChipDescription()
                {
                    Position = new int2(0,1),
                    Color = ChipColor.Blue,
                },
                new SlotChipDescription()
                {
                    Position = new int2(1,1),
                    Color = ChipColor.Red,
                },
                new SlotChipDescription()
                {
                    Position = new int2(2,1),
                    Color = ChipColor.Blue,
                },

                new SlotChipDescription()
                {
                    Position = new int2(0,2),
                    Color = ChipColor.Yellow,
                },
                new SlotChipDescription()
                {
                    Position = new int2(1,2),
                    Color = ChipColor.Yellow,
                },
                new SlotChipDescription()
                {
                    Position = new int2(2,2),
                    Color = ChipColor.Yellow,
                },

            }
        };

        var sceneConfiguration = CreateTestSceneConfiguration();
        var configuration = CreateTestConfiguration();
        var slotCache = new Dictionary<int2, Entity>();
        var createSlots = new CreateSlotsStep(slotCache, sceneConfiguration);
        createSlots.Apply(levelDescription, entityManager);


        var createChips = new CreateChipsStep(configuration);
        createChips.Apply(levelDescription, entityManager);

        var combinationList = new NativeList<Entity>(64, Allocator.Temp);
        var visited = new NativeHashMap<int2, bool>(64, Allocator.Temp);

        FindCombinationsSystem.Find(entityManager, slotCache, new int2(0,0), ref visited, ref combinationList);
        Assert.AreEqual(3, combinationList.Length);
        Assert.IsFalse(FindCombinationsSystem.IsCorrectCombination(entityManager, combinationList, levelDescription.Width, levelDescription.Height));

        visited.Dispose();
        combinationList.Dispose();
    }
}