using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace ExampleAssembly
{
    public class Loader: MonoBehaviour
    {
        static UnityEngine.GameObject gameObject;
        static Player player;
        private static List<Tuple<BuildingType, long>> matReqs;

        private static void Log(string text)
        {
            var dt = DateTime.Now.ToString("G");
            File.AppendAllText (@"c:\SL\ngui_log.txt", $"{dt} {text}\n");
        }



        private static void FillMap(int mapId, BuildingType buildingType)
        {
            Log($"Filling map {mapId} with buildings {buildingType}");
            if (!player.factoryData.maps[mapId].unlocked)
            {
                Log("Map still locked. Exiting.");
                return;
            }
            player.factoryController.setNewMapID(mapId);
            Log($"Set new map id to {mapId}");
            Thread.Sleep(1000);
            player.factoryController.doWipeCurrentMap();
            Log($"Wiped current map");
            Thread.Sleep(1000);
            for (int i = 0; i < player.factory.maps[mapId].tiles.Length; i++)
            {
                var t = player.factory.maps[mapId].tiles[i];                
                if (t.terrain == TerrainType.Open || player.factoryData.maps[mapId].clearedTiles.Contains(i))
                {
                    //player.factoryData.maps[mapId].buildings.Add(new TileData(i, buildingType));
                    player.factoryController.setNewTilestate(i, buildingType, TileDirection.Up);
                }
            }
            Log($"Done adding buildings");
            Thread.Sleep(1000);
            player.factoryController.updateTilePods();
            player.factoryController.tracker.onTilestateChange();
            Log("Updated state");
        }

        private static void FillAllMaps(BuildingType bt)
        {
            var buMID = player.factoryController.curMapID;
            for (int m = 0; m < player.factoryData.maps.Count; m++)
            {
                FillMap(m, bt);
            }
            Thread.Sleep(500);
            player.factoryController.setNewMapID(buMID);
        }

        private static void CalculateMatReqs(BuildingType bt, long amount)
        {
            foreach (var mat in player.factoryController.buildingProperties.properties[(int)bt].requiredMats)
            {
                long reqAmount = mat.amount * amount;
                if (player.materials.materials[(int)mat.material].amount < reqAmount)
                {
                    if (matReqs.FindAll(x => x.Item1 == mat.material).Count == 0)
                    {
                        matReqs.Add(new Tuple<BuildingType, long>(mat.material, 0));
                    }
                    var item = matReqs.Find(x => x.Item1 == mat.material);
                    var idx = matReqs.IndexOf(item);                    
                    matReqs[idx] = new Tuple<BuildingType, long>(mat.material, matReqs[idx].Item2 + reqAmount);
                    CalculateMatReqs(mat.material, reqAmount);
                }
            }
        }

        private static void Make(BuildingType bt, long amount, bool root = true)
        {
            Log($"Making {amount} {bt}.");
            if (player.materials.materials[(int)bt].amount >= amount)
            {
                Log("Nothing to make. Exiting");
                return;
            }
            //check reqs
            if (root)
            {
                matReqs = new List<Tuple<BuildingType, long>>();
                CalculateMatReqs(bt, amount);
                Log($"Matreqs count {matReqs.Count}");
                foreach(var mr in matReqs)
                {
                    Log($"{mr.Item1} => {mr.Item2}");
                }
                /*for (int i = matReqs.Count - 1; i >= 0; i--)
                {
                    Log($"Making required submat {matReqs[i].Item1}");
                    Make(matReqs[i].Item1, matReqs[i].Item2, false);
                }*/
            }
            foreach (var mat in player.factoryController.buildingProperties.properties[(int)bt].requiredMats)
            {
                var item = matReqs.Find(x => x.Item1 == mat.material);
                var reqAmount = Math.Max(item == null? 0 : item.Item2, mat.amount * amount);
                if (player.materials.materials[(int)mat.material].amount < reqAmount)
                {
                    Log($"Making required submat {mat.material}");
                    Make(mat.material, reqAmount, false);
                }
            }

            FillAllMaps(bt);
            while (true)
            {
                if (player.materials.materials[(int)bt].amount >= amount)
                {
                    Log($"Done making {amount} {bt}");
                    break; 
                }
                Thread.Sleep(5000);
            }
        }

        public static void CheatMats()
        {
            for (int i = 0; i < player.materials.materials.Count; i++)
            {
                player.materials.materials[i].amount += 1000000000000;
            }
        }

        public static void Load()
        {
            try
            {
                gameObject = new UnityEngine.GameObject();
                gameObject.AddComponent<Cheat>();
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                player = FindObjectOfType<Player>();
                Log("Player " + player.playerName);
                Log("Unlocked bases " + player.playerBase.unlockedBases.Count);


                //FillAllMaps(BuildingType.CopperOre);


                /* for (int m = 0; m < 4; m++)
                 {

                     player.factoryData.maps[m].unlocked = true;
                     player.factoryController.setNewMapID(m);
                     Thread.Sleep(1000);
                     for (int i = 0; i < player.factory.maps[m].tiles.Length; i++)
                     {
                         var t = player.factory.maps[m].tiles[i];
                         //if (t.terrain == TerrainType.Open )
                         {
                             //player.factoryData.maps[mapId].buildings.Add(new TileData(i, buildingType));
                             player.factoryController.setNewTilestate(i, BuildingType.None, TileDirection.Up);
                         }
                     }
                 }*/


                //Make(BuildingType.FleshJuice2, 10);
                CheatMats();

                //Completion log
                /*  foreach (var bb in player.allBuffs.buildingBuffs)
                  {
                      var s = "";
                      foreach (var ba in bb)
                          s += ", " + ba;
                      Log("Building buff " + bb + " list: " + s);
                  }*/
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                Log(ex.StackTrace);
            }

        }

        public static void Unload()
        {
            UnityEngine.Object.Destroy(gameObject);
        }
    }
}
