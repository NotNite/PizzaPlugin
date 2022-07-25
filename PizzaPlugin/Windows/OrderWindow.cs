using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;
using PizzaPlugin.API;

namespace PizzaPlugin.Windows;

public class OrderWindow : Window, IDisposable {
    private Address address = new("3535 S Ball St", "Arlington", "Virginia", "22202", Country.UnitedStates,
        ServiceType.Delivery);

    private Store? store;
    private Menu? menu;

    public OrderWindow() : base("OrderWindow") {
        Size = new Vector2(810, 520);
    }

    public void Dispose() { }

    public override void OnOpen() {
        store = address.GetClosestStore();
        menu = store.GetMenu();
    }

    public override void Draw() {
        if (menu != null) {
            foreach (var item in menu.Items) {
                ImGui.Text($"{item.Name} - {item.Price}");
            }
        } else {
            ImGui.Text("No menu available");
        }
    }
}