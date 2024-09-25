// estimate
export async function estimate() {
  return navigator.storage.estimate();
}

// const root = await navigator.storage.getDirectory();
export async function getDirectory() {
  return navigator.storage.getDirectory();
}
