import {jest} from '@jest/globals';
import {indexedDB, IDBKeyRange} from "fake-indexeddb";
import {
  openIndexedDB,
  upgradeDatabase,
  createIndex,
  clearStore,
  getAll,
  getAllByIndex,
  getOne,
  addOne,
  addMany,
  updateOne,
  deleteOne,
  count,
  beginTransaction,
  commitTransaction
} from '../../../library/IdxDb/wwwroot/idb-old.mjs';

describe('IndexedDB Module Tests', () => {
  const dbName = 'TestDB';
  const storeName = 'items';
  const timeout = 1000;

  beforeAll(async () => {
    // Upgrade the database and create the object store before running tests
    await openIndexedDB(dbName, 1, (db) => {
      if (!db.objectStoreNames.contains(storeName)) {
        db.createObjectStore(storeName, { keyPath: 'id' });
      }
    });
  }, timeout);

  afterEach(async () => {
    // Clear the object store after each test to ensure isolation
    const db = await openIndexedDB(dbName);
    const transaction = db.transaction(storeName, 'readwrite');
    const store = transaction.objectStore(storeName);
    store.clear();
    await new Promise((resolve, reject) => {
      transaction.oncomplete = resolve;
      transaction.onerror = () => reject(transaction.error);
    });
  }, timeout);

  test('openIndexedDB should open and cache the database', async () => {
    const db = await openIndexedDB(dbName, 1);
    expect(db).toBeDefined();
    expect(db.name).toBe(dbName);
    expect(db.version).toBe(1);

    // Attempt to open again and ensure it's cached
    const cachedDb = await openIndexedDB(dbName);
    expect(cachedDb).toBe(db);
  }, timeout);

  test('upgradeDatabase should create new object stores and indexes', async () => {
    const upgradeDbName = 'UpgradeTestDB';
    const newVersion = 2;
    const newStoreName = 'users';
    const storeSchemas = [
      {
        name: newStoreName,
        options: { keyPath: 'userId' },
        indexes: [
          { name: 'nameIndex', keyPath: 'name', unique: false },
          { name: 'emailIndex', keyPath: 'email', unique: true },
        ],
      },
    ];

    await upgradeDatabase(upgradeDbName, newVersion, storeSchemas);

    const db = await openIndexedDB(upgradeDbName, newVersion);
    expect(db.objectStoreNames.contains(newStoreName)).toBe(true);

    const transaction = db.transaction(newStoreName, 'readonly');
    const store = transaction.objectStore(newStoreName);
    expect(store.indexNames.contains('nameIndex')).toBe(true);
    expect(store.indexNames.contains('emailIndex')).toBe(true);
  });

  test('getAll should retrieve all items', async () => {
    const items = [
      { id: 7, name: 'Item 7' },
      { id: 8, name: 'Item 8' },
      { id: 9, name: 'Item 9' },
    ];

    await addMany(dbName, storeName, items);

    const allItems = await getAll(dbName, storeName);
    expect(allItems).toHaveLength(3);
    expect(allItems).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ id: 7, name: 'Item 7' }),
        expect.objectContaining({ id: 8, name: 'Item 8' }),
        expect.objectContaining({ id: 9, name: 'Item 9' }),
      ])
    );
  });
  
  test('addOne should add an item and getOne should retrieve it', async () => {
    const item = { id: 1, name: 'Test Item' };
    const addResult = await addOne(dbName, storeName, item);
    expect(addResult).toBe(true);

    const fetchedItem = await getOne(dbName, storeName, 1);
    expect(fetchedItem).toEqual(item);
  }, timeout);

  test('updateOne should update an existing item', async () => {
    const originalItem = { id: 2, name: 'Original Item' };
    const updatedItem = { id: 2, name: 'Updated Item' };

    // Add the original item
    const addResult = await addOne(dbName, storeName, originalItem);
    expect(addResult).toBe(true);

    // Update the item
    const updateResult = await updateOne(dbName, storeName, updatedItem);
    expect(updateResult).toBe(true);

    // Retrieve the updated item
    const fetchedItem = await getOne(dbName, storeName, 2);
    expect(fetchedItem).toEqual(updatedItem);
  });

  test('deleteOne should remove an item', async () => {
    const item = { id: 3, name: 'Item to Delete' };

    // Add the item
    const addResult = await addOne(dbName, storeName, item);
    expect(addResult).toBe(true);

    // Delete the item
    const deleteResult = await deleteOne(dbName, storeName, 3);
    expect(deleteResult).toBe(true);

    // Attempt to retrieve the deleted item
    const fetchedItem = await getOne(dbName, storeName, 3);
    expect(fetchedItem).toBeUndefined();
  });

  test('addMany should add multiple items', async () => {
    const items = [
      { id: 4, name: 'Item 4' },
      { id: 5, name: 'Item 5' },
      { id: 6, name: 'Item 6' },
    ];

    const addManyResult = await addMany(dbName, storeName, items);
    expect(addManyResult).toBe(true);

    const allItems = await getAll(dbName, storeName);
    expect(allItems).toHaveLength(3);
    expect(allItems).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ id: 4, name: 'Item 4' }),
        expect.objectContaining({ id: 5, name: 'Item 5' }),
        expect.objectContaining({ id: 6, name: 'Item 6' }),
      ])
    );
  });

  // test('createIndex should add a new index and getAllByIndex should retrieve items using the index', async () => {
  //   const indexStoreName = 'products';
  //   const indexDbName = 'IndexTestDB';
  //   const indexName = 'categoryIndex';
  //   const storeSchemas = [
  //     {
  //       name: indexStoreName,
  //       options: { keyPath: 'productId' },
  //     },
  //   ];
  //
  //   // Upgrade the database to create the object store
  //   await upgradeDatabase(indexDbName, 1, storeSchemas);
  //
  //   // Create an index on 'category'
  //   await createIndex(indexDbName, indexStoreName, indexName, 'category', false);
  //
  //   const db = await openIndexedDB(indexDbName, 2); // Increment version to trigger upgrade
  //   const store = db.transaction(indexStoreName, 'readonly').objectStore(indexStoreName);
  //   expect(store.indexNames.contains(indexName)).toBe(true); // This should now pass
  //
  //   // Add items
  //   const products = [
  //     { productId: 101, name: 'Laptop', category: 'Electronics' },
  //     { productId: 102, name: 'Shirt', category: 'Apparel' },
  //     { productId: 103, name: 'Smartphone', category: 'Electronics' },
  //   ];
  //   await addMany(indexDbName, indexStoreName, products);
  //
  //   // Retrieve items by category 'Electronics'
  //   const electronics = await getAllByIndex(indexDbName, indexStoreName, indexName, 'Electronics');
  //   expect(electronics).toHaveLength(2);
  //   expect(electronics).toEqual(
  //     expect.arrayContaining([
  //       expect.objectContaining({ productId: 101, name: 'Laptop', category: 'Electronics' }),
  //       expect.objectContaining({ productId: 103, name: 'Smartphone', category: 'Electronics' }),
  //     ])
  //   );
  // }, timeout);

  test('beginTransaction and commitTransaction should manage transactions correctly', async () => {
    const transactionStoreName = 'transactions';
    const transactionDbName = 'TransactionTestDB';
    const items = [
      { id: 201, name: 'Transaction Item 1' },
      { id: 202, name: 'Transaction Item 2' },
    ];

    // Upgrade the database to create the object store
    await upgradeDatabase(transactionDbName, 1, [
      {
        name: transactionStoreName,
        options: { keyPath: 'id' },
      },
    ]);

    // Begin a transaction and add items
    await beginTransaction(transactionDbName, transactionStoreName, 'readwrite');
    const db = await openIndexedDB(transactionDbName);
    const transaction = db.transaction(transactionStoreName, 'readwrite');
    const store = transaction.objectStore(transactionStoreName);

    items.forEach((item) => {
      store.add(item);
    });

    // Commit the transaction
    await commitTransaction();

    // Verify that items were added
    const allItems = await getAll(transactionDbName, transactionStoreName);
    expect(allItems).toHaveLength(2);
    expect(allItems).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ id: 201, name: 'Transaction Item 1' }),
        expect.objectContaining({ id: 202, name: 'Transaction Item 2' }),
      ])
    );
  });

  test('count should return the correct number of records', async () => {
    const countStoreName = 'countStore';
    const countDbName = 'CountTestDB';
    const items = [
      { id: 301, name: 'Count Item 1' },
      { id: 302, name: 'Count Item 2' },
      { id: 303, name: 'Count Item 3' },
    ];

    // Upgrade the database to create the object store
    await upgradeDatabase(countDbName, 1, [
      {
        name: countStoreName,
        options: { keyPath: 'id' },
      },
    ]);

    // Add items
    await addMany(countDbName, countStoreName, items);

    // Count records
    const recordCount = await count(countDbName, countStoreName);
    expect(recordCount).toBe(3);
  });

  test('clearStore should remove all records from the store', async () => {
    const clearStoreName = 'clearStore';
    const clearDbName = 'ClearTestDB';
    const items = [
      { id: 401, name: 'Clear Item 1' },
      { id: 402, name: 'Clear Item 2' },
    ];

    // Upgrade the database to create the object store
    await upgradeDatabase(clearDbName, 1, [
      {
        name: clearStoreName,
        options: { keyPath: 'id' },
      },
    ]);

    // Add items
    await addMany(clearDbName, clearStoreName, items);

    // Clear the store
    const clearResult = await clearStore(clearDbName, clearStoreName);
    expect(clearResult).toBe(true);

    // Verify the store is empty
    const allItems = await getAll(clearDbName, clearStoreName);
    expect(allItems).toHaveLength(0);
  });
});
