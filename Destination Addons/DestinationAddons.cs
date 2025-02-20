using Life;
using System;
using Life.Network;
using Life.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Life.VehicleSystem;
using ModKit.Helper;
using ModKit.Interfaces;
using _menu = AAMenu.Menu;
using Life.BizSystem;
using System.IO;
using Newtonsoft.Json;
using Logger = ModKit.Internal.Logger;

namespace Destination_Addons
{
    public class DestinationAddons: ModKit.ModKit
    {
        // Initialisation de l'emplacement des fichiers json
        public static string timeconfig = "time.json";
        public static string persodest = "dest.json";

        // Initialisation des variables utilisées
        public int intervalle = 3;
        public Dictionary<Vehicle, string[]> vehicles = new Dictionary<Vehicle, string[]>();
        public List<string> destListSaved = new List<string>()
        {
            "Déviation sur votre ligne",
            "L'entrée du bus se fait à l'avant",
            " << Joyeuses Fêtes >> ",
            "Nous recrutons des chauffeurs !",
            "Service Limité"
        };
        public List<string> destList = new List<string>();

        public static string directoryPath;

        public void SaveTimeJson()
        {
            string jsonFile = Directory.GetFiles(directoryPath, timeconfig).FirstOrDefault();
            string updatedJson = JsonConvert.SerializeObject(intervalle, Formatting.Indented);
            File.WriteAllText(jsonFile, updatedJson);
        }

        public void SaveDestJson()
        {
            string jsonFile = Directory.GetFiles(directoryPath, persodest).FirstOrDefault();
            string updatedJson = JsonConvert.SerializeObject(destListSaved, Formatting.Indented);
            File.WriteAllText(jsonFile, updatedJson);
        }

        public DestinationAddons(IGameAPI api) : base(api) // Constructeur
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.2.2", "IceCubeFr");
        }


        public override void OnPluginInit() // Démarrage du serveur
        {
            // Initialisation de la structure de base

            base.OnPluginInit();
            InsertMenu();
            InitDirectory();
            // Initialisation du json time

            try
            {
                string jsonFile = Directory.GetFiles(directoryPath, timeconfig).FirstOrDefault();

                if (jsonFile != null)
                {
                    string json = File.ReadAllText(jsonFile);
                    int setup = JsonConvert.DeserializeObject<int>(json);
                    intervalle = setup;
                    SaveTimeJson();
                }
                else
                {
                    int info = intervalle;
                    string filePath = Path.Combine(directoryPath, timeconfig);
                    string json = JsonConvert.SerializeObject(info, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[PLUGIN] Failed to load Json file from Destination Board, time.json : " + ex.Message);
            }


            // Initialisation du json dest

            try
            {
                string jsonFile = Directory.GetFiles(directoryPath, persodest).FirstOrDefault();

                if (jsonFile != null)
                {
                    string json = File.ReadAllText(jsonFile);
                    List<string> setup = JsonConvert.DeserializeObject<List<string>>(json);
                    destListSaved = setup;
                    SaveDestJson();
                }
                else
                {
                    string filePath = Path.Combine(directoryPath, persodest);
                    string json = JsonConvert.SerializeObject(destListSaved, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[PLUGIN] Failed to load Json file from Destination Board, dest.json : " + ex.Message);
            }


            // Initialisation de la commande d'ouverture du menu

            new SChatCommand("/destination", new string [1] {"/dest"}, "Ouvre le menu de gestion des destinations du bus", "/dest(ination)", (player, arg) =>
            {
                if (player.HasBiz)
                {
                    if (player.biz.IsActivity(Activity.Type.Bus))
                    {
                        Panel(player);
                    }
                }

            }).Register();

            // Fin de l'initialisation

            ModKit.Internal.Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "succesfully started !");
        }

        public void InsertMenu()
        {
            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Bus }, null, "Gestion girouette", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                Panel(player);
            });
            _menu.AddAdminTabLine(PluginInformations, 5, "Destination Addons", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                EditIntervalle(player);
            });
        }

        public void CancelAddon(Vehicle veh)
        {
            if (vehicles.ContainsKey(veh))
            {
                veh.bus.NetworkgirouetteText = vehicles[veh][0];
                vehicles.Remove(veh);
            }
        }

        public IEnumerator DestUpdate()
        {
            while (true)
            {
                foreach (Vehicle vehicule in vehicles.Keys)
                {
                    
                    if (destList.Contains(vehicule.bus.girouetteText))
                    {
                        vehicule.bus.NetworkgirouetteText = vehicles[vehicule][0];
                        
                    }
                    else if (vehicule.bus.girouetteText == vehicles[vehicule][0])
                    {
                        vehicule.bus.NetworkgirouetteText = vehicles[vehicule][1];
                    }
                    else
                    {
                        vehicles[vehicule][0] = vehicule.bus.girouetteText;
                    }
                }
                yield return new WaitForSeconds(intervalle);
            }
            
        }

        public void Launch(Player player, string girouette, Vehicle veh)
        {
            player.setup.StopAllCoroutines();
            if (!destList.Contains(girouette)) { destList.Add(girouette); }
            if (vehicles.ContainsKey(veh))
            {
                vehicles[veh][1] = girouette;
            }
            else
            {
                vehicles.Add(veh, new String[] {veh.bus.girouetteText, girouette});
            }
            player.setup.StartCoroutine(DestUpdate());
            player.Notify("Destination ajoutée", "La destination a été ajoutée", NotificationManager.Type.Success);
        }

        public void Save(Player player, Vehicle veh, string girouette)
        {
            if (!destListSaved.Contains(girouette))
            {
                destListSaved.Add(girouette);
                SaveDestJson();
                player.Notify("Success", "Girouette sauvegardée", NotificationManager.Type.Success);
            }
            else
            {
                player.Notify("Information", "La girouette était déjà sauvegardée");
            }
            
            Launch(player, girouette, veh);
        }

        public void AskSave(Player player, Vehicle veh, string girouette)
        {
            Panel save = PanelHelper.Create("Sauvegarder la destination ?", UIPanel.PanelType.Text, player, () => AskSave(player, veh, girouette));
            save.TextLines.Add("Souhaitez-vous sauvegarder la girouette pour plus tard ?");
            save.AddButton("Non", ui =>
            {
                Launch(player, girouette, veh);
                player.ClosePanel(save);
            });
            save.AddButton("Oui", ui =>
            {
                Save(player, veh, girouette);
                player.ClosePanel(save);
            });
            save.Display();
        }

        public void ConfigAutres(Player player, Vehicle veh)
        {
            Panel configAutres = PanelHelper.Create("Quel texte souhaitez vous afficher en plus ?", UIPanel.PanelType.Input, player, () => ConfigAutres(player, veh));

            configAutres.CloseButton();
            configAutres.AddButton("Valider", ui => AskSave(player, veh, configAutres.inputText));
            configAutres.PreviousButton();

            configAutres.Display();
        }

        public void ValidationDelete(Player player, string girouette)
        {
            Panel validation = PanelHelper.Create("Validation", UIPanel.PanelType.Text, player, () => ValidationDelete(player, girouette));
            validation.TextLines.Add("Êtes vous sûrs de vouloir supprimer cette girouette ?");
            validation.TextLines.Add("<color=#FF0000>Attention ! Cette action est irréversible !</color>");
            validation.PreviousButton();
            validation.AddButton("<color=#FF0000>Supprimer</color>", ui =>
            {
                destListSaved.Remove(girouette);
                SaveDestJson();
                player.Notify("Girouette supprimée", "La girouette a été supprimée.", NotificationManager.Type.Warning);
                Panel(player);
            });
            validation.Display();
        }

        public void DeleteGirouette(Player player)
        {
            Panel delete = PanelHelper.Create("Sélectionnez une girouette à supprimer", UIPanel.PanelType.Tab, player, () => DeleteGirouette(player));
            foreach (string destination in destListSaved)
            {
                delete.AddTabLine(destination, ui => ValidationDelete(player, destination));
            }
            delete.PreviousButton();
            delete.AddButton("Valider", ui => delete.SelectTab());
            delete.Display();
        }

        public void Panel(Player player)
        {
            string vehModel = player.GetVehicleModel();
            Vehicle veh = player.GetClosestVehicle();
            if (vehModel != null && vehModel == "Euro Lion's City 12")
            {
                // Config du panel
                Panel panel = PanelHelper.Create("Sélectionnez un affichage supplémentaire", UIPanel.PanelType.Tab, player, () => Panel(player));

                // Config des tabs
                panel.AddTabLine("Aucun", ui =>
                {
                    CancelAddon(veh);
                    player.Notify("Destination supprimée", "La destination supplémentaire a bien été retirée", NotificationManager.Type.Success);
                });
                foreach (string destination in destListSaved)
                {
                    panel.AddTabLine(destination, ui => Launch(player, destination, veh));
                }
                panel.AddTabLine("Autre (texte personnalisé)", ui => ConfigAutres(player, veh));
                if (player.biz.OwnerId == player.character.Id)
                {
                    panel.AddTabLine("Supprimer une girouette", ui => DeleteGirouette(player));
                }

                // Config des bouttons
                panel.CloseButton();
                panel.AddButton("Valider", ui => panel.SelectTab());

                // Display
                panel.Display();
            }
            else
            {
                player.Notify("Erreur", "Vous n'êtes pas dans un bus", NotificationManager.Type.Error);
            }
        }

        public void EditIntervalle(Player player)
        {
            Panel iedit = PanelHelper.Create("Modifier Intervalle", UIPanel.PanelType.Input, player, () => EditIntervalle(player));
            iedit.SetInputPlaceholder($"Temps actuel : {intervalle} secondes");
            iedit.PreviousButton();
            iedit.AddButton("Valider", ui =>
            {
                if (int.TryParse(iedit.inputText, out int interval) && interval > 0)
                {
                    intervalle = interval;
                    SaveTimeJson();
                    player.ClosePanel(iedit);
                    player.Notify("Success", "Intervalle modifiée avec succès !", NotificationManager.Type.Success);
                }
                else
                {
                    EditIntervalle(player);
                    player.Notify("Erreur", "Veuillez renseigner un nombre entier positif", NotificationManager.Type.Error);
                }
            });
            iedit.Display();
        }

        public void InitDirectory()
        {
            directoryPath = pluginsPath + "/DestionationAddons";
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }
    }
}
