# Infinity Blade Save Dumper

The **Infinity Blade Save Dumper** tool converts serialized Infinity Blade saves into a JSON file so you can make highly customizable changes.  
It also has a feature that re-calculates and serializes your deserialized save, making it fast and easy to change and import back into your game.

---

## 📖 How to Use

###  Deserialization (Convert `.bin` ➝ `.json`)
1. Launch the program.
2. Drag and drop an **unencrypted serialized `.bin` save file** into the program window.
3. Press **Enter**.
4. The tool will **deserialize** the save and create a `.json` file in the **`OUTPUT`** folder.

---

###  Serialization (Convert `.json` ➝ `.bin`)
1. Launch the program.
2. Drag and drop a **modified `.json` save file** into the program window.
3. Press **Enter**.
4. The tool will **serialize** the save and create a `.bin` file in the **`OUTPUT`** folder.

---

## 📂 Output Location
- All converted files will be stored in the **`OUTPUT`** directory (created automatically if it doesn’t exist).
  This means you do not need to create a backup of the save you're modifying since the new data will be packaged into an entirely new file.

---

## ⚠️ Notes
- Currently only works with **unencrypted** save files. Planning to add encrypted save support in the future.
- Program is currently set to only deserialize IB3 saves. This is of course changable, but **seamless** modification to all file types is not implemented yet.
