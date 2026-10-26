import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { FoodItemDto, StoreAisleDto } from '../../models/food-catalog.models';
import { FoodCatalogService } from './food-catalog.service';

describe('FoodCatalogService', () => {
  let service: FoodCatalogService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), FoodCatalogService],
    });

    service = TestBed.inject(FoodCatalogService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('fetches aisles via GET /api/store-aisles', async () => {
    const aisles: StoreAisleDto[] = [
      { id: '1', name: 'Produce', sortOrder: 1, isSystemSeeded: true },
    ];

    const promise = firstValueFrom(service.getStoreAisles());
    const req = httpMock.expectOne('/api/store-aisles');
    expect(req.request.method).toBe('GET');
    req.flush(aisles);

    const result = await promise;
    expect(result).toEqual(aisles);
  });

  it('fetches items with query parameters via GET /api/food-items', async () => {
    const promise = firstValueFrom(
      service.getFoodItems({ search: 'garlic', isPantryStaple: true }),
    );
    const req = httpMock.expectOne('/api/food-items?search=garlic&isPantryStaple=true');
    expect(req.request.method).toBe('GET');
    req.flush([]);

    const result = await promise;
    expect(result).toEqual([]);
  });

  it('creates food item via POST /api/food-items', async () => {
    const item: FoodItemDto = {
      id: 'f-1',
      name: 'Shallot',
      defaultStoreAisleId: null,
      defaultStoreAisleName: null,
      defaultStoreAisleSortOrder: null,
      isPantryStaple: false,
      aliases: ['French Shallot'],
      referencedRecipeCount: 0,
      createdAt: '2026-01-01T00:00:00Z',
      modifiedAt: '2026-01-01T00:00:00Z',
    };

    const promise = firstValueFrom(
      service.createFoodItem({ name: 'Shallot', aliases: ['French Shallot'] }),
    );
    const req = httpMock.expectOne('/api/food-items');
    expect(req.request.method).toBe('POST');
    req.flush(item);

    const result = await promise;
    expect(result.name).toBe('Shallot');
  });
});
