// indexedDB open
export async function open(name, version) {
  console.log(`Opening IndexedDB ${name} version ${version}`);
  const db = indexedDB.open(name, version);
  
  return new Promise((resolve, reject) => {
    db.onsuccess = (event) => {
      resolve(event.target.result);
    };
    db.onerror = (event) => {
      reject(event.target.error);
    };
  });
}