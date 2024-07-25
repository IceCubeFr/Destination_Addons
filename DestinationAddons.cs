using Life;
using Life.Network;
using Life.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Life.VehicleSystem;
using ModKit.Helper;
using ModKit.Interfaces;
using _menu = AAMenu.Menu;
using Life.BizSystem;

namespace Destination_Addons
{
    public class DestinationAddons: ModKit.ModKit
    {

        string destSelect = "";

        public List<string> destList = new List<string>();

        public DestinationAddons(IGameAPI api) : base(api) // Constructeur
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.1", "IceCubeFr");
        }


        public override void OnPluginInit() // Démarrage du serveur
        {
            base.OnPluginInit();
            InsertMenu();

            // Commande de modification de la touche de base

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

            destList.Add("Déviation sur votre ligne");
            destList.Add("L'entrée du bus se fait à l'avant");
            destList.Add(" << Joyeuses Fêtes >> ");
            destList.Add("Nous recrutons des chauffeurs !");

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
        }

        public bool IsDestValid(Vehicle veh)
        {
            return !destList.Contains(veh.bus.girouetteText);
        }

        public IEnumerator DestUpdate(Vehicle vehicule)
        {
            string dest = vehicule.bus.girouetteText;
            int id = 0;
            while (true)
            {
                if (id == 0)
                {
                    vehicule.bus.NetworkgirouetteText = dest;
                    id = 1;
                }
                else
                {
                    vehicule.bus.NetworkgirouetteText = destSelect;
                    id = 0;
                }
                yield return new WaitForSeconds(3.0f);
            }
            
        }

        public void Launch(Player player, int girouette, Vehicle veh)
        {
            if (IsDestValid(veh))
            {
                player.setup.StopAllCoroutines();
                destSelect = destList[girouette];
                player.setup.StartCoroutine(DestUpdate(veh));
                player.Notify("Destination ajoutée", "La destination a été ajoutée", NotificationManager.Type.Success);
            }
            else
            {
                player.Notify("Attention", "Veuillez réessayer dans 3 secondes");
            }
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
            configAutres.AddButton("Valider", ui => AjoutAutres(player, veh, configAutres.inputText));
            configAutres.PreviousButton();

            configAutres.Display();
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
                panel.AddTabLine("Déviation", ui => Launch(player, 0, veh));
                panel.AddTabLine("Entrée à l'avant", ui => Launch(player, 1, veh));
                panel.AddTabLine("Joyeuses fêtes", ui => Launch(player, 2, veh));
                panel.AddTabLine("Recrutement chauffeurs", ui => Launch(player, 3, veh));
                panel.AddTabLine("Autre (texte personnalisé)", ui => ConfigAutres(player, veh));

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
    }
}
