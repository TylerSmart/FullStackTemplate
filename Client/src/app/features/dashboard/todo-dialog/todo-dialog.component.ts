import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TodoItemDto } from '../../../core/models/todo.models';

export interface TodoDialogData {
  todo?: TodoItemDto;
}

export interface TodoDialogResult {
  title: string;
  description: string | null;
}

@Component({
  imports: [
    ReactiveFormsModule,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  selector: 'app-todo-dialog',
  templateUrl: './todo-dialog.component.html',
  styleUrl: './todo-dialog.component.scss',
})
export class TodoDialogComponent {
  protected readonly data = inject<TodoDialogData | null>(MAT_DIALOG_DATA, { optional: true });
  protected readonly dialogRef = inject(MatDialogRef<TodoDialogComponent, TodoDialogResult>);

  protected readonly isEditMode = Boolean(this.data?.todo);

  protected readonly form = new FormGroup({
    title: new FormControl(this.data?.todo?.title ?? '', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    description: new FormControl(this.data?.todo?.description ?? '', {
      nonNullable: true,
      validators: [Validators.maxLength(1000)],
    }),
  });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.dialogRef.close({
      title: raw.title.trim(),
      description: raw.description.trim() || null,
    });
  }
}
