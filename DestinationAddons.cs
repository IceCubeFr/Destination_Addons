using Life;
using System;
using Life.Network;
using Life.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Life.VehicleSystem;
using ModKit.Helper;
using ModKit.Interfaces;
using _menu = AAMenu.Menu;
using Life.BizSystem;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics.Eventing.Reader;

namespace Destination_Addons
{
    public class DestinationAddons: ModKit.ModKit
    {
        // Initialisation de l'emplacement des fichiers json
        public static string timeconfig = "time.json";
        public static string persodest = "dest.json";

        // Initialisation des variables utilisées
        public int intervalle = 3;
        public int duration = 3;
        string destSelect = "";
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
            List<int> info = new List<int>
                        {
                            duration,
                            intervalle
                        };
            string jsonFile = Directory.GetFiles(directoryPath, timeconfig).FirstOrDefault();
            string updatedJson = JsonConvert.SerializeObject(info, Formatting.Indented);
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
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.2.1", "IceCubeFr");
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
                    List<int> setup = JsonConvert.DeserializeObject<List<int>>(json);
                    duration = setup[0];
                    intervalle = setup[1];
                    SaveTimeJson();
                }
                else
                {
                    List<int> info = new List<int>
                        {
                            duration,
                            intervalle
                        };
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
                if (player.HasBiz())
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
            _menu.AddAdminTabLine(PluginInformations, 5, "Paramètres Destination Addons", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                AdminMenu(player);
            });
        }

        public bool IsDestValid(Vehicle veh)
        {
            return !destList.Contains(veh.bus.girouetteText);
        }

        public IEnumerator DestUpdate(Vehicle vehicule)
        {
            string dest = vehicule.bus.girouetteText;
            int id = 0;
            int time = intervalle;
            while (true)
            {
                if (id == 0)
                {
                    vehicule.bus.NetworkgirouetteText = dest;
                    id = 1;
                    time = intervalle;
                }
                else
                {
                    vehicule.bus.NetworkgirouetteText = destSelect;
                    id = 0;
                    time = duration;
                }
                yield return new WaitForSeconds(time);
            }
            
        }

        public void Launch(Player player, string girouette, Vehicle veh)
        {
            if (IsDestValid(veh))
            {
                if (!destList.Contains(girouette)) { destList.Add(girouette); }
                player.setup.StopAllCoroutines();
                destSelect = girouette;
                player.setup.StartCoroutine(DestUpdate(veh));
                player.Notify("Destination ajoutée", "La destination a été ajoutée", NotificationManager.Type.Success);
            }
            else
            {
                player.Notify("Attention", "Veuillez réessayer dans 3 secondes");
            }
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
            
            AjoutAutres(player, veh, girouette);
        }

        public void AskSave(Player player, Vehicle veh, string girouette)
        {
            Panel save = PanelHelper.Create("Sauvegarder la destination ?", UIPanel.PanelType.Text, player, () => AskSave(player, veh, girouette));
            save.TextLines.Add("Souhaitez-vous sauvegarder la girouette pour plus tard ?");
            save.AddButton("Non", ui =>
            {
                if (IsDestValid(veh))
                {
                    AjoutAutres(player, veh, girouette);
                    player.ClosePanel(save);
                }
                else
                {
                    player.Notify("Attention", "Veuillez réessayer dans 3 secondes");
                }

            });
            save.AddButton("Oui", ui =>
            {
                Save(player, veh, girouette);
                player.ClosePanel(save);
            });
            save.Display();
        }

        public void AjoutAutres(Player player, Vehicle vehicle, string text)
        {
            if (IsDestValid(vehicle))
            {
                player.setup.StopAllCoroutines();
                destSelect = text;
                player.setup.StartCoroutine(DestUpdate(vehicle));
                if (!destList.Contains(text))
                {
                    destList.Add(text);
                }
                player.Notify("Destination ajoutée", "La destination a été ajoutée", NotificationManager.Type.Success);
            }
            else
            {
                player.Notify("Attention", "Veuillez réessayer dans 3 secondes");
            }
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
                    if (IsDestValid(veh))
                    {
                        player.setup.StopAllCoroutines();
                        player.Notify("Destination supprimée", "La destination supplémentaire a bien été retirée", NotificationManager.Type.Success);
                    }
                    else
                    {
                        player.Notify("Attention", "Veuillez réessayer dans 3 secondes");
                    }
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
                    AdminMenu(player);
                    player.Notify("Success", "Intervalle modifiée avec succès !", NotificationManager.Type.Success);
                }
                else
                {
                    player.Notify("Erreur", "Veuillez renseigner un nombre entier positif", NotificationManager.Type.Error);
                }
            });
            iedit.Display();
        }

        public void EditDuration(Player player)
        {
            Panel dedit = PanelHelper.Create("Modifier Intervalle", UIPanel.PanelType.Input, player, () => EditDuration(player));
            dedit.SetInputPlaceholder($"Temps actuel : {duration} secondes");
            dedit.PreviousButton();
            dedit.AddButton("Valider", ui =>
            {
                if (int.TryParse(dedit.inputText, out int time) && time > 0)
                {
                    duration = time;
                    SaveTimeJson();
                    AdminMenu(player);
                    player.Notify("Success", "Durée modifiée avec succès !", NotificationManager.Type.Success);
                }
                else
                {
                    player.Notify("Erreur", "Veuillez renseigner un nombre entier positif", NotificationManager.Type.Error);
                }
            });
            dedit.Display();
        }

        public void AdminMenu(Player player)
        {
            Panel adminpanel = PanelHelper.Create("Gestion Admin", UIPanel.PanelType.Tab, player, () => AdminMenu(player));
            adminpanel.AddTabLine("Modifier intervalle", ui => EditIntervalle(player));
            adminpanel.AddTabLine("Modifier durée d'affichage", ui => EditDuration(player));
            adminpanel.CloseButton();
            adminpanel.AddButton("Valider", ui =>  adminpanel.SelectTab());
            adminpanel.Display();
        }

        public void InitDirectory()
        {
            directoryPath = pluginsPath + "/DestionationAddons";
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }
    }
}
