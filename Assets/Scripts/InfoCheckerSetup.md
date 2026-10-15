# Territory UI setup

1. Keep InfoChecker on the camera.
2. Create a Canvas with a TextMeshPro input field. Assign it to **Territory Input** on InfoChecker. The script configures multiline JSON editing.
3. Create two UI Buttons labeled **Import** and **Export** and assign them to the matching InfoChecker fields. The script registers their click handlers; leave their Inspector On Click lists empty to avoid duplicate calls.
4. Assign the root map GameObject to **Map**. Every map descendant is included, including inactive children; temporary selection outline objects are excluded. Objects with territory data need a Territory component. Clicking also finds Territory on a collider's parent.
5. Optionally assign a second TMP text object to **Status Text** for import/export results. Otherwise messages appear in the Console.
6. Ensure the scene has an EventSystem with InputSystemUIInputModule and the Canvas has a GraphicRaycaster. Clickable territory objects need colliders.

Selected data displays as formatted JSON. New public fields/properties on TerritoryModel automatically appear and round-trip. Keep new custom data classes in Models or Assets.Models, or extend the serializer's allowed model namespaces. Do not mark members JsonIgnore if they need to be saved.

Export creates a unique Territories-*.json in the user's Downloads directory on Windows, macOS, and Linux (including redirected Windows Downloads and Linux XDG configuration). Import opens a runtime file browser with folders, a path field, and JSON files. This is a desktop feature; browser/mobile builds need their platform's download/file-picker integration.

Import expects this export format and the same map names, sibling order, and hierarchy. It validates all children before replacing runtime models. It preserves cyclic/shared neighbor references and concrete piece types. It does not create scene objects, add Territory components, rewrite modelString, or persist changes to the scene asset; export again to retain imported/runtime edits.

The selection indicator follows the selected MeshRenderer mesh silhouette, including flat territory meshes, and is visible through geometry. Territory objects must have a MeshRenderer, MeshFilter, and MeshCollider. Adjust Outline Color and Outline Width (world units in the camera plane) in the Inspector. It follows the object without modifying its original materials. The URP shader reserves stencil bit 64 for the selected mesh mask; other custom stencil effects must avoid that bit. Transparent or alpha-cutout textures are outlined by mesh geometry rather than texture opacity.

Validation: the scripts compile against the project's Unity 6000.1 assemblies. A standalone JSON round-trip check passed for cyclic/shared neighbors, derived pieces, model values, duplicate object names, non-territory children, and rejection of unknown JSON types. Verify the Canvas layout, button clicks, file browser, and outline in Play Mode.

Territory editing: Assign a TMP_InputField to Territory Input. Select a territory, edit its JSON, and click outside the field to apply. Invalid JSON leaves the live model unchanged and the draft visible for correction; selecting another territory discards that draft. Status Text shows results (or the Console when no status label is assigned). Edits replace the selected component's runtime model, including nested neighbor data from the JSON; they do not update other components that referenced the previous model. Use Export to retain changes after Play Mode.

Import also accepts older exports containing temporary Selection mask, Selection border, or Selection outline entries beneath territories. Other hierarchy mismatches are rejected with the affected name and path.

Selection clicks: selection happens on left-button release only when the press and release hit the same Territory and the pointer never moves beyond Selection Drag Threshold (default 5 pixels). Dragging preserves the current selection, even if the pointer returns to its starting position. UI interactions, the import browser, and focus loss cancel pending clicks. Clicking empty space without dragging clears selection.

Selecting a territory calls Territory.FindNeighbors before refreshing its JSON display. Unless the neighbor list has been manually edited, it checks the mesh edges for nearby colliders belonging to other Territory components, returns their GameObjects without duplicates, and replaces the selected model's Neighbors with their live models. Neighbor Distance on Territory sets the maximum gap in world units (default 0.03); adjust it to the map scale. Mesh Read/Write must be enabled in the model importer. Missing or unreadable meshes retain the existing neighbor list and log a warning. Discovery runs only on selection, including reselection, and does not update the other territories' neighbor lists. The UI shows neighbor names; map exports retain full model data.

A successful UI edit that changes Neighbors marks that territory's list as manually edited. FindNeighbors then preserves the entire list, including additions, replacements, removals, and an empty or null list. Editing other fields leaves automatic discovery enabled unless neighbors were already manually edited. Exports save NeighborsManuallyEdited alongside each map entry, and imports restore it with the neighbor data. Older exports without the flag default to automatic discovery.
