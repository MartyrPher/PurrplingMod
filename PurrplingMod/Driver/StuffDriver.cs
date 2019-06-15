﻿using Microsoft.Xna.Framework;
using PurrplingMod.Model;
using PurrplingMod.Objects;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingMod.Driver
{
    class StuffDriver
    {
        public List<BagDumpInfo> DumpedBags { get; set; }
        public IMonitor Monitor { get; }

        public StuffDriver(IModEvents events, IDataHelper dataHelper, IMonitor monitor)
        {
            events.GameLoop.Saving += this.GameLoop_Saving;
            events.GameLoop.SaveLoaded += this.GameLoop_SaveLoaded;

            this.DataHelper = dataHelper;
            this.DumpedBags = new List<BagDumpInfo>();
            this.Monitor = monitor;
        }

        public void DetectAndPrepareBagsToSave()
        {
            FarmHouse house = Game1.getLocationFromName("FarmHouse") as FarmHouse;
            //Dictionary<Vector2, StardewValley.Object> toSwap = new Dictionary<Vector2, StardewValley.Object>();

            this.DumpedBags.Clear();

            foreach (var objKv in house.objects.Pairs)
            {
                if (!(objKv.Value is DumpedBag bag))
                    continue;

                Vector2 chestPosition = bag.TileLocation;
                BagDumpInfo bagInfo = new BagDumpInfo()
                {
                    source = bag.GivenFrom,
                    giftboxIndex = bag.giftboxIndex.Value,
                    message = bag.Message,
                    posX = (int)objKv.Key.X,
                    posY = (int)objKv.Key.Y,
                };

                Chest chest = new Chest(true);
                chest.TileLocation = bag.TileLocation;
                chest.items.AddRange(bag.items);

                house.objects[objKv.Key] = chest;

                this.DumpedBags.Add(bagInfo);
                this.Monitor.Log($"Found bag to save from ${bagInfo.source} at position {bagInfo.posX},{bagInfo.posY} with {chest.items.Count} items");
            }

            /*foreach (var toSwapKv in toSwap)
            {
                house.objects.Remove(toSwapKv.Key);
                house.objects.Add(toSwapKv.Key, toSwapKv.Value);
            }*/

            this.Monitor.Log($"Detected {this.DumpedBags.Count} to save.");
        }

        public IDataHelper DataHelper { get; }

        public void RevivePossibleBags()
        {
            FarmHouse house = Game1.getLocationFromName("FarmHouse") as FarmHouse;
            foreach(BagDumpInfo bagInfo in this.DumpedBags)
            {
                Vector2 position = new Vector2(bagInfo.posX, bagInfo.posY);

                if (!house.objects.TryGetValue(position, out StardewValley.Object obj) && !(obj is Chest))
                {
                    this.Monitor.Log($"Bag at position ${position} can't be revived!", LogLevel.Warn);
                    continue;
                }

                Chest chest = obj as Chest;
                DumpedBag bag = new DumpedBag(chest.items.ToList(), position, 0);

                bag.GivenFrom = bagInfo.source;
                bag.Message = bagInfo.message;

                house.objects[position] = bag;

                this.Monitor.Log($"Revive dumpedBag on position ${bag.TileLocation} (Items count: {bag.items.Count})");
            }

            this.Monitor.Log("Dumped bags revived!", LogLevel.Info);
        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            try
            {
                List<BagDumpInfo> dumpedBags = this.DataHelper.ReadSaveData<List<BagDumpInfo>>("dumped-bags");
                this.DumpedBags = dumpedBags ?? new List<BagDumpInfo>();
                this.Monitor.Log($"Count of possible bags: {this.DumpedBags.Count}");
                this.Monitor.Log("Dumped bags loaded from save file", LogLevel.Info);
            }
            catch (InvalidOperationException ex)
            {
                this.Monitor.Log($"Error while loading dumped bag from savefile: {ex.Message}");
            }
        }

        private void GameLoop_Saving(object sender, SavingEventArgs e)
        {
            try
            {
                this.DataHelper.WriteSaveData("dumped-bags", this.DumpedBags ?? new List<BagDumpInfo>());
                this.Monitor.Log("Dumped bags successfully saved to savefile.", LogLevel.Info);
            }
            catch (InvalidOperationException ex)
            {
                this.Monitor.Log($"Error while saving dumped bags: {ex.Message}", LogLevel.Error);
            }
        }
    }
}
