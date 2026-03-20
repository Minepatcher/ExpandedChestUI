### v1.1.1
- Allow static usage of ExpandedChestActionsClient for better performance/usage.
- Fixed issue where items would not go to the player's inventory.

### v1.1.0
- Added a new ExpandedButtonUIElement component to allow for custom button UI.
- Added Shortcut Keys and their implementation to each of the chest buttons.
- Removed BurstDisabler from InventoryUpdateSystem.
- Added New PugSimulationSystemBase to handle the creation of new Inventory Change Actions.

### v1.0.0
- Change the namespace naming convention to be more consistent.
- Add four new buttons based on @reishyousose's Quality of Core Keeper buttons.
- Create TextDataBlocks for five buttons for consistent capitalization.
- Created burst override/disabling for InventoryUpdateSystem to override and create new Inventory Change Actions.

### v0.4.0
- Use copies of in-game files for textures due to visual differences in the in-game compressed files.
- In-game texture addresses removed due to mod file size bloating.
- Scrolling with mouse now scrolls by half the slot height.
- Controller Support updated to use right thumbstick for scrolling. Allowing user to not have to scroll through the entire chest inventory to get to their player inventory.
- Fixed Initial item load issue where items with an bars did not show their exp, usage, or reinforcement bars.

### v0.3.0
- Update to Core Keeper 1.2
- Use new sprites now available on the SDK.
- Remove unused sprites.

### v0.2.1
- Bug Fix: Tool Damage and Pet Exp will now show *still working out an initial load bug, but opening a second chest will fix.
- Enhancement: Initial Controller Support (not working the way I'd like, but is good to get it going.)

### v0.2.0
- Bug Fix: Pets are no longer in Grayscale. #5
- Bug Fix: Pet Achievement is now achievable even with this mod.
- Bug Fix: Scroll will now reset to top each time you enter the chest instead of keeping the last scroll input.
- Enhancement: Added Item Slot Animations

### v0.1.0
- Enhancement: Any extra buttons added to the chest UI by other mods are now handled and will be displayed in a grid. (Ex: Quality of Core Keeper Mod)

### v0.0.2
- Fix: When focused on the chest inventory. The out-of-bounds slots will sometimes cover the player inventory slots.

### v0.0.1
- Initial Release